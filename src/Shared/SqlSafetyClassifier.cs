using System.Text;
using System.Text.RegularExpressions;

// One source file, compiled into BOTH the Api and the Web project (linked from each .csproj), so the
// rule that decides what SQL may be run - and how a card describes it - can never drift apart.
namespace AIforMAsupport.Shared;

public enum SqlRisk
{
    // Only reads data.
    ReadOnly,

    // Changes rows (DELETE / UPDATE / INSERT / MERGE). Runs only when writes are switched on.
    Modifies,

    // Never runnable from this tool, whoever wrote it and whatever the settings: it changes
    // structure or permissions, calls procedures, or does something else the tool has no business
    // doing. Copy it and run it yourself in SSMS if it is really needed.
    Blocked,
}

public sealed record SqlClassification(SqlRisk Risk, string? Reason);

// Decides what a piece of SQL is allowed to do. The API calls this on the EXACT text it is about to
// send to SQL Server (after the DECLARE lines and the [MoCS]. qualifier are gone) - classifying
// anything else could disagree with what actually runs (a block comment opened before a removed
// DECLARE line closes differently once that line is gone). The Web project calls it on displayed SQL
// only to label buttons; the API's answer is the one that counts.
//
// The check reads the SQL the way SQL Server does before it looks for keywords: comments (block
// comments nest in T-SQL) and string literals are removed first, so "-- drop table" or
// 'DELETE FROM x' inside a string is not mistaken for a command, and a command hidden behind a
// comment trick is not missed.
public static partial class SqlSafetyClassifier
{
    // A line that is nothing but "DECLARE @Name TYPE = 'value'". The API turns these into bound
    // parameters (the value never becomes SQL text) and removes the line; Classify must see the SQL
    // without them, so both use this one pattern.
    [GeneratedRegex(
        @"^\s*DECLARE\s+@(?<name>\w+)\s+\w+(?:\([^)]*\))?\s*=\s*'(?<default>[^']*)'\s*$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    public static partial Regex DeclareLine();

    // For SQL as displayed to a person (DECLARE lines and manual transaction lines still in it): the
    // risk of what would run.
    public static SqlClassification ClassifyDisplayed(string sql) =>
        Classify(RemoveTransactionControlLines(DeclareLine().Replace(sql ?? string.Empty, string.Empty)));

    // A line that is nothing but BEGIN TRAN / COMMIT / ROLLBACK (with optional TRAN[SACTION], a
    // name, and a semicolon). Such lines are how a person drives a transaction by hand in SSMS
    // ("run the check, look at @@ROWCOUNT, then COMMIT or ROLLBACK") - the API already runs every
    // write inside its own transaction that it rolls back on any error, so they are removed before
    // running. Leaving them in would let the script's own BEGIN TRAN stay open with no COMMIT (the
    // AI often writes the COMMIT only as a comment), and the whole change would be silently rolled
    // back when the connection is released while the screen reported success.
    [GeneratedRegex(
        @"^[ \t]*(?:BEGIN[ \t]+TRAN(?:SACTION)?(?:[ \t]+\w+)?|COMMIT(?:[ \t]+TRAN(?:SACTION)?(?:[ \t]+\w+)?)?|ROLLBACK(?:[ \t]+TRAN(?:SACTION)?(?:[ \t]+\w+)?)?)[ \t]*;?[ \t]*\r?$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TransactionControlLine();

    public static string RemoveTransactionControlLines(string sql) =>
        TransactionControlLine().Replace(sql ?? string.Empty, string.Empty);

    // Transaction control anywhere else (inline, e.g. "IF @@ROWCOUNT <> 1 ROLLBACK") cannot be
    // safely removed and must not run inside the API's own transaction.
    [GeneratedRegex(
        @"(?<![A-Za-z0-9_@#$])(?:BEGIN[ \t\r\n]+TRAN(?:SACTION)?|COMMIT|ROLLBACK|SAVE[ \t\r\n]+TRAN(?:SACTION)?)(?![A-Za-z0-9_@#$])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InlineTransactionControl();

    // For SQL exactly as it will be sent to SQL Server.
    public static SqlClassification Classify(string executableSql)
    {
        var code = StripNonCode(executableSql ?? string.Empty).TrimStart(' ', '\t', '\r', '\n', '\f', '\v', ';');
        if (code.Length == 0)
        {
            return new SqlClassification(SqlRisk.ReadOnly, null);
        }

        // In T-SQL a procedure can be run without the word EXEC when it is the first statement of
        // a batch ("MyProc 1, 2"). Requiring the batch to start with a known statement keyword closes
        // that door: nothing here would spot a plain procedure name.
        var first = FirstWord().Match(code);
        if (!first.Success || !AllowedFirstWords.Contains(first.Value))
        {
            return new SqlClassification(
                SqlRisk.Blocked,
                "SQL ต้องขึ้นต้นด้วยคำสั่งอย่าง SELECT / WITH / DECLARE / SET / DELETE / UPDATE / INSERT / MERGE เท่านั้น (ระบบไม่อนุญาตให้รันชื่อโพรซีเดอร์ตรง ๆ)");
        }

        var blocked = BlockedKeyword().Match(code);
        if (blocked.Success)
        {
            return new SqlClassification(
                SqlRisk.Blocked,
                $"SQL นี้มีคำสั่ง {blocked.Value.ToUpperInvariant()} ซึ่งระบบไม่อนุญาตให้รัน (สร้าง/แก้โครงสร้าง, รันโพรซีเดอร์, สิทธิ์ หรือคำสั่งระบบ) — ถ้าจำเป็นจริง ให้คัดลอกไปรันเองใน SSMS");
        }

        if (InlineTransactionControl().IsMatch(code))
        {
            return new SqlClassification(
                SqlRisk.Blocked,
                "SQL นี้มีคำสั่งควบคุม transaction เอง (BEGIN TRAN / COMMIT / ROLLBACK) — ระบบห่อ transaction ให้อยู่แล้วและย้อนกลับอัตโนมัติเมื่อผิดพลาด ให้ลบคำสั่งเหล่านี้ออก (ถ้าเป็นบรรทัดเดี่ยว ๆ ระบบตัดออกให้เอง)");
        }

        var selectInto = SelectInto().Match(code);
        if (selectInto.Success)
        {
            return new SqlClassification(
                SqlRisk.Blocked,
                "SQL นี้มี SELECT ... INTO (สร้างตารางใหม่) ซึ่งระบบไม่อนุญาตให้รัน — ถ้าจำเป็นจริง ให้คัดลอกไปรันเองใน SSMS");
        }

        return WriteKeyword().IsMatch(code)
            ? new SqlClassification(SqlRisk.Modifies, null)
            : new SqlClassification(SqlRisk.ReadOnly, null);
    }

    private static readonly HashSet<string> AllowedFirstWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "WITH", "DECLARE", "SET", "DELETE", "UPDATE", "INSERT", "MERGE",
    };

    // Structure and permission changes, procedure/dynamic-SQL execution, server-level and
    // file/remote-access commands. "USE" would switch the connection to another database.
    [GeneratedRegex(
        @"(?<![A-Za-z0-9_@#$])(?:CREATE|ALTER|DROP|TRUNCATE|EXEC|EXECUTE|GRANT|REVOKE|DENY|BACKUP|RESTORE|SHUTDOWN|DBCC|KILL|RECONFIGURE|OPENROWSET|OPENQUERY|OPENDATASOURCE|BULK|WAITFOR|USE|SETUSER|CHECKPOINT)(?![A-Za-z0-9_@#$])|(?<![A-Za-z0-9_])\[?(?:xp|sp)_\w+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockedKeyword();

    // "SELECT ... INTO x" creates a table; "INSERT INTO x" is an ordinary write and is not this.
    [GeneratedRegex(@"(?<!\bINSERT\s+)\bINTO\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectInto();

    [GeneratedRegex(@"\b(?:DELETE|UPDATE|INSERT|MERGE)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WriteKeyword();

    [GeneratedRegex(@"^[A-Za-z]+")]
    private static partial Regex FirstWord();

    // Removes what SQL Server ignores or treats as data, leaving what it would read as commands:
    //   -- line comments (to the end of the line)
    //   /* block comments */ - which NEST in T-SQL: /* a /* b */ c */ is one comment
    //   'string literals' ('' is an escaped quote) - content dropped, the quotes kept
    // [bracketed] and "quoted" identifiers are copied through untouched (a "--" inside them is not a
    // comment), so a keyword used as a column name is still seen - the safe direction. An
    // unterminated comment or string swallows the rest, which SQL Server itself rejects as a syntax
    // error, so nothing runs.
    private static string StripNonCode(string sql)
    {
        var sb = new StringBuilder(sql.Length);
        var i = 0;
        var n = sql.Length;

        while (i < n)
        {
            var c = sql[i];

            if (c == '-' && i + 1 < n && sql[i + 1] == '-')
            {
                i += 2;
                while (i < n && !IsLineEnd(sql[i]))
                {
                    i++;
                }

                sb.Append(' ');
                continue;
            }

            if (c == '/' && i + 1 < n && sql[i + 1] == '*')
            {
                var depth = 1;
                i += 2;
                while (i < n && depth > 0)
                {
                    if (sql[i] == '/' && i + 1 < n && sql[i + 1] == '*')
                    {
                        depth++;
                        i += 2;
                    }
                    else if (sql[i] == '*' && i + 1 < n && sql[i + 1] == '/')
                    {
                        depth--;
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }

                sb.Append(' ');
                continue;
            }

            if (c == '\'')
            {
                i++;
                while (i < n)
                {
                    if (sql[i] == '\'')
                    {
                        if (i + 1 < n && sql[i + 1] == '\'')
                        {
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    i++;
                }

                sb.Append("''");
                continue;
            }

            if (c == '[' || c == '"')
            {
                var close = c == '[' ? ']' : '"';
                sb.Append(c);
                i++;
                while (i < n)
                {
                    sb.Append(sql[i]);
                    if (sql[i] == close)
                    {
                        if (i + 1 < n && sql[i + 1] == close)
                        {
                            sb.Append(close);
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    i++;
                }

                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    // More line-ending characters than SQL Server may recognise: ending a "--" comment early only
    // means more text is read as commands, which is the safe direction.
    private static bool IsLineEnd(char c) =>
        c is '\n' or '\r' or '\f' or '\v' || c == (char)0x0085 || c == (char)0x2028 || c == (char)0x2029;
}
