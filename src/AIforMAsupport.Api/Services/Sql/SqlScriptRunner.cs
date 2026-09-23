using System.Text.RegularExpressions;
using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.KnownScripts;
using AIforMAsupport.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.Sql;

// Executes real SQL against the database - either a KnownScript looked up by name (a .sql file on
// disk under KnownScripts/, see IKnownScriptRepository) or, since the user explicitly chose to
// allow it, raw SQL text supplied directly (used for SQL the AI wrote itself and marked
// "-- [AI-SQL:UNVERIFIED]" - see RunnableSqlFormatter/SimpleMarkdown on the Web side).
//
// DELETE/UPDATE/INSERT can run for real too now (the user explicitly asked for this, understanding
// the risk, and confirmed the target database - MoCS_dev - is a genuinely separate copy/instance
// from production, not production itself) - gated by its own SqlRunOptions.AllowWrites switch,
// independent of the read-only Enabled switch, so writes specifically can be killed without
// losing read-only SQL Studio. A write execution is wrapped in a real ADO.NET transaction and
// rolled back on any failure, so a multi-statement script can't partially apply. DECLARE lines are
// stripped out of the SQL text and replaced with real ADO.NET SqlParameter bindings instead of
// string substitution, so a value typed into a placeholder field can never break out of its
// parameter - it can only match (or fail to match) a value.
public sealed partial class SqlScriptRunner : ISqlScriptRunner
{
    private readonly IKnownScriptRepository _scripts;
    private readonly IConfiguration _configuration;
    private readonly SqlRunOptions _options;
    private readonly ILogger<SqlScriptRunner> _logger;

    public SqlScriptRunner(
        IKnownScriptRepository scripts,
        IConfiguration configuration,
        IOptions<SqlRunOptions> options,
        ILogger<SqlScriptRunner> logger)
    {
        _scripts = scripts;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SqlRunResponse> RunAsync(
        string? scriptName,
        string? sqlText,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new SqlRunNotAllowedException("ฟีเจอร์รันสคริปต์จริงยังไม่ได้เปิดใช้งาน (SqlRun:Enabled = false)");
        }

        string sqlContent;
        string label;
        if (!string.IsNullOrWhiteSpace(scriptName))
        {
            var script = _scripts.GetAll().FirstOrDefault(s => s.Name == scriptName);
            if (script is null)
            {
                throw new SqlRunNotAllowedException($"ไม่พบสคริปต์ '{scriptName}' ใน Known Scripts");
            }

            sqlContent = script.SqlContent;
            label = scriptName;
        }
        else if (!string.IsNullOrWhiteSpace(sqlText))
        {
            sqlContent = sqlText;
            label = "(AI-authored SQL)";
        }
        else
        {
            throw new SqlRunNotAllowedException("ต้องระบุ scriptName หรือ sqlText อย่างใดอย่างหนึ่ง");
        }

        // Re-checked here even though the UI already disables the button for unfilled values -
        // never trust the client for this. A "<placeholder>" token or an empty DECLARE value means
        // the SQL isn't ready to run against real data yet.
        if (HasUnfilledAnglePlaceholder(sqlContent))
        {
            throw new SqlRunNotAllowedException(
                "SQL นี้ยังมีค่าที่ต้องระบุ (เช่น <วันที่> หรือ <LoadingNo>) — ระบุค่าจริงให้ครบก่อนจึงจะรันจริงได้");
        }

        var connectionString = _configuration.GetConnectionString(_options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new SqlRunNotAllowedException($"ไม่พบ connection string '{_options.ConnectionStringName}'");
        }

        var (commandText, dbParameters) = BuildCommand(sqlContent, parameters);

        // What may run is decided on commandText - the exact text about to be sent to SQL Server -
        // and never on the SQL as the caller wrote it: the DECLARE lines and the [MoCS]. qualifier
        // are removed on the way, and a comment opened before a removed line can close differently
        // once it is gone. Anything structural, procedural or permission-related is refused for
        // everyone, always; row changes need SqlRun:AllowWrites.
        var classification = SqlSafetyClassifier.Classify(commandText);
        if (classification.Risk == SqlRisk.Blocked)
        {
            throw new SqlRunNotAllowedException(classification.Reason!);
        }

        var isWrite = classification.Risk == SqlRisk.Modifies;
        if (isWrite && !_options.AllowWrites)
        {
            throw new SqlRunNotAllowedException(
                "SQL นี้มีคำสั่งแก้ไขข้อมูล (DELETE/UPDATE/INSERT/MERGE) และฟีเจอร์รันจริงสำหรับคำสั่งแก้ไขข้อมูลยังไม่ได้เปิดใช้งาน (SqlRun:AllowWrites = false) — คัดลอกไปรันเองเท่านั้น");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        SqlTransaction? transaction = isWrite
            ? (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.Parameters.AddRange(dbParameters.ToArray());
            if (transaction is not null)
            {
                command.Transaction = transaction;
            }

            var tables = new List<SqlResultTable>();
            int recordsAffected;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                do
                {
                    // A bare DELETE/UPDATE/INSERT (no attached SELECT) reports FieldCount=0 for
                    // its "result" - skip adding an empty table for those; the rows-affected
                    // summary below covers them instead.
                    if (reader.FieldCount > 0)
                    {
                        tables.Add(await ReadTableAsync(reader, cancellationToken));
                    }
                } while (await reader.NextResultAsync(cancellationToken));

                recordsAffected = reader.RecordsAffected;
            }

            // Give each result grid a heading taken from the comment written above its SELECT in the
            // script itself, so the SQL file is the single source of truth for what each table is.
            // Only applied when the count lines up one-to-one with the script's top-level SELECTs;
            // otherwise the grids keep the generic "ผลลัพธ์ที่ N" label rather than risk mislabelling.
            var headings = ExtractSelectHeadings(sqlContent);
            if (headings.Count == tables.Count)
            {
                for (var i = 0; i < tables.Count; i++)
                {
                    if (headings[i] is { } heading)
                    {
                        tables[i] = tables[i] with { Label = heading };
                    }
                }
            }

            if (isWrite)
            {
                tables.Add(new SqlResultTable(
                    "ผลการแก้ไขข้อมูล",
                    ["ผลลัพธ์"],
                    [[recordsAffected >= 0 ? $"สำเร็จ — มีผลกระทบ {recordsAffected} แถว" : "สำเร็จ (ระบบไม่รายงานจำนวนแถวที่กระทบ)"]]));
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Ran live SQL {Label} (write={IsWrite}) - {TableCount} result set(s)", label, isWrite, tables.Count);

            return new SqlRunResponse(tables);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    // One entry per top-level SELECT statement (a line that starts with SELECT), in script order -
    // the same order the result sets come back in. The heading is the run of comment lines
    // directly above the statement (no blank line in between); pure separator lines made of
    // dashes/equals signs are skipped. A SELECT with no comment above it gets null.
    private static List<string?> ExtractSelectHeadings(string sql)
    {
        var lines = sql.Replace("\r\n", "\n").Split('\n');
        var headings = new List<string?>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (!SelectStatementStart().IsMatch(lines[i]))
            {
                continue;
            }

            var parts = new List<string>();
            for (var j = i - 1; j >= 0; j--)
            {
                var trimmed = lines[j].Trim();
                if (!trimmed.StartsWith("--"))
                {
                    break;
                }

                var text = trimmed.TrimStart('-').Trim();
                // Separator lines, and the "[AI-SQL:UNVERIFIED]" marker the AI puts at the top of its
                // own SQL (sometimes again before a second statement) - neither describes the table.
                if (text.Trim('-', '=', ' ').Length == 0
                    || text.Equals("[AI-SQL:UNVERIFIED]", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                parts.Insert(0, text);
            }

            headings.Add(parts.Count == 0 ? null : string.Join(" — ", parts));
        }

        return headings;
    }

    [GeneratedRegex(@"^\s*SELECT\b", RegexOptions.IgnoreCase)]
    private static partial Regex SelectStatementStart();

    private async Task<SqlResultTable> ReadTableAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<IReadOnlyList<string?>>();

        while (rows.Count < _options.MaxRowsPerTable && await reader.ReadAsync(cancellationToken))
        {
            var row = new string?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
            }

            rows.Add(row);
        }

        return new SqlResultTable(null, columns, rows);
    }

    // Replaces each "DECLARE @Var TYPE = '...'" line with nothing (ADO.NET supplies the value as
    // a real bound parameter instead of text), and resolves every declared variable name to the
    // caller-supplied value - falling back to the script's own default when the caller didn't
    // send one, matching the same fallback SQL Studio already applies client-side for Copy.
    //
    // Also strips the hardcoded "[MoCS]." database qualifier every KnownScripts/*.sql file uses
    // in front of its three-part table names (e.g. "[MoCS].[dbo].[Plan]"), and that the AI has
    // been instructed to drop too when it writes its own SQL (see MoCS-Schema-Reference.md) - the
    // scripts were authored assuming the real production database is literally named "MoCS", but
    // the connection string here can point at a differently-named copy (e.g. "MoCS_dev") - SQL
    // Server treats "[MoCS]" as a cross-database reference to a database that doesn't exist under
    // that exact name and fails with "Invalid object name". Dropping the qualifier leaves
    // two-part "[dbo].[Plan]" names, which resolve against whatever database the connection
    // string's Database= already points at.
    private static (string CommandText, List<SqlParameter> Parameters) BuildCommand(
        string sqlContent,
        IReadOnlyDictionary<string, string> parameters)
    {
        var dbParameters = new List<SqlParameter>();
        var commandText = SqlSafetyClassifier.DeclareLine().Replace(sqlContent, match =>
        {
            var name = match.Groups["name"].Value;
            var defaultValue = match.Groups["default"].Value;
            var value = parameters.TryGetValue(name, out var supplied) && !string.IsNullOrEmpty(supplied)
                ? supplied
                : defaultValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new SqlRunNotAllowedException($"ยังไม่ได้ระบุค่าให้ @{name} — กรอกค่าให้ครบก่อนจึงจะรันจริงได้");
            }

            dbParameters.Add(new SqlParameter("@" + name, value));
            return string.Empty;
        });

        // Manual BEGIN TRAN / COMMIT / ROLLBACK lines (how a person drives a transaction by hand in
        // SSMS) are removed: every write already runs inside this method's own transaction, and a
        // leftover BEGIN TRAN with no COMMIT would be rolled back silently on release.
        commandText = SqlSafetyClassifier.RemoveTransactionControlLines(commandText);

        commandText = MoCsDatabaseQualifier().Replace(commandText, string.Empty);

        return (commandText, dbParameters);
    }

    // Comments are stripped first so a note like "-- แทน <วันที่ยืนยัน> ด้วยค่าจริง" doesn't count;
    // only "<word...>" tokens in real SQL do. "<>" / "<=" operators don't match (a letter must
    // follow the "<").
    private static bool HasUnfilledAnglePlaceholder(string sql)
    {
        var withoutComments = LineComment().Replace(BlockComment().Replace(sql, " "), string.Empty);
        return AnglePlaceholder().IsMatch(withoutComments);
    }

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"<\p{L}[^<>\r\n]{0,60}?>")]
    private static partial Regex AnglePlaceholder();

    [GeneratedRegex(@"\[MoCS\]\.(?=\[)", RegexOptions.IgnoreCase)]
    private static partial Regex MoCsDatabaseQualifier();
}
