using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.Sql;

// Two files, both read once at startup (neither changes at runtime):
//   - MoCS-Schema-Reference.md: short hand-written notes, always included in full.
//   - MoCS-Schema-Full.md: every table/view in the database (generated from INFORMATION_SCHEMA),
//     split into one "### schema.Table (TABLE|VIEW)" block each. Far too big to send whole
//     (hundreds of tables), so per request only the blocks whose table names actually appear in
//     the text being reasoned about are included - see GetSchemaText.
public sealed partial class FileDbSchemaProvider : IDbSchemaProvider
{
    private sealed record TableBlock(string FullName, string ShortName, string Text, IReadOnlyList<string> Referenced, Regex NamePattern);

    private readonly DbSchemaOptions _options;
    private readonly string _referenceText;
    private readonly List<TableBlock> _tables = [];
    private readonly Dictionary<string, TableBlock> _byFullName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<TableBlock>> _byShortName = new(StringComparer.OrdinalIgnoreCase);

    public FileDbSchemaProvider(IOptions<DbSchemaOptions> options, ILogger<FileDbSchemaProvider> logger)
    {
        _options = options.Value;

        var referencePath = Path.GetFullPath(_options.FilePath);
        if (File.Exists(referencePath))
        {
            _referenceText = File.ReadAllText(referencePath);
        }
        else
        {
            logger.LogWarning("DB schema reference file not found: {Path}", referencePath);
            _referenceText = string.Empty;
        }

        var fullPath = Path.GetFullPath(_options.FullFilePath);
        if (File.Exists(fullPath))
        {
            LoadFullSchema(File.ReadAllLines(fullPath));
            logger.LogInformation("Loaded full DB schema: {TableCount} tables/views from {Path}", _tables.Count, fullPath);
        }
        else
        {
            logger.LogWarning("Full DB schema file not found: {Path} - only the short reference notes will be used", fullPath);
        }
    }

    public SchemaSelection GetSchema(
        string priorityText,
        string contextText,
        IReadOnlyList<string>? caseTexts = null,
        IReadOnlyCollection<string>? carriedTables = null)
    {
        var selected = SelectTables(priorityText ?? string.Empty, contextText ?? string.Empty, caseTexts, carriedTables);
        if (selected.Count == 0)
        {
            return new SchemaSelection(_referenceText, []);
        }

        var sb = new StringBuilder(_referenceText);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("## ตารางที่เกี่ยวข้องกับคำถามนี้ (โครงสร้างจริงจากฐานข้อมูล คอลัมน์ครบ — คอลัมน์ที่ไม่ระบุ null = NOT NULL)");
        sb.AppendLine();
        foreach (var table in selected)
        {
            sb.AppendLine(table.Text);
        }

        return new SchemaSelection(sb.ToString(), selected.Select(t => t.FullName).ToList());
    }

    // Kept below a single piece of fresh SQL evidence (5): carrying tables over should keep a
    // follow-up on the same tables, not let a conversation's earlier tables crowd out a change of
    // topic. Above passing mentions (at most 3), so a table that was in use isn't dropped just
    // because the new message doesn't repeat its name.
    private const int CarriedTableBonus = 4;

    private List<TableBlock> SelectTables(
        string priorityText,
        string contextText,
        IReadOnlyList<string>? caseTexts,
        IReadOnlyCollection<string>? carriedTables)
    {
        // MaxTables = 0 switches the per-question table selection off entirely (only the short
        // reference notes are sent) - the quick way to compare answer speed with and without it.
        if (_tables.Count == 0 || _options.MaxTables <= 0)
        {
            return [];
        }

        // For each retrieved case, which tables its own SQL reads or changes (FROM / JOIN /
        // UPDATE / DELETE / INSERT INTO ...) - the strongest hint of where the team fixed this
        // kind of problem before. Counted per case, so a table used by several of the matched
        // cases outranks one that a single case happens to touch.
        var sqlCaseCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var caseText in caseTexts ?? [])
        {
            foreach (var fullName in ExtractSqlTables(caseText))
            {
                sqlCaseCounts[fullName] = sqlCaseCounts.GetValueOrDefault(fullName) + 1;
            }
        }

        // Score each table by how often its exact (case-sensitive, identifier-bounded) name shows
        // up. Names in the Known Scripts' SQL count for much more than a passing mention in prose.
        var carriedSet = new HashSet<string>(carriedTables ?? [], StringComparer.OrdinalIgnoreCase);
        var scored = new List<(TableBlock Table, int Score)>();
        var strong = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in _tables)
        {
            var priority = CountMatches(table, priorityText);
            var context = CountMatches(table, contextText);
            var sqlCases = Math.Min(sqlCaseCounts.GetValueOrDefault(table.FullName), 6);
            var carried = carriedSet.Contains(table.FullName) ? CarriedTableBonus : 0;
            var score = priority * 5 + context + sqlCases * 5 + carried;
            if (score > 0)
            {
                scored.Add((table, score));
            }

            // Named in a matched script, used in a matched case's (or the previous answer's) SQL,
            // or already in use earlier in the conversation - not just mentioned in passing. Only
            // these are worth following foreign keys out of (see below).
            if (priority > 0 || sqlCases > 0 || carried > 0)
            {
                strong.Add(table.FullName);
            }
        }

        var ordered = scored
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Table.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(s => s.Table)
            .ToList();

        var selected = new List<TableBlock>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var chars = 0;

        bool TryAdd(TableBlock table)
        {
            if (selected.Count >= _options.MaxTables || seen.Contains(table.FullName))
            {
                return false;
            }

            // The first table is always allowed even if it alone exceeds the char budget.
            if (selected.Count > 0 && chars + table.Text.Length > _options.MaxChars)
            {
                return false;
            }

            selected.Add(table);
            seen.Add(table.FullName);
            chars += table.Text.Length;
            return true;
        }

        // Tables backed by a script or a matched case's SQL go in first. Tables that were only
        // mentioned in passing come after, and only a few of them: they are the noisiest signal
        // (common words like Plan/Delivery) and every extra table lengthens the prompt.
        foreach (var table in ordered.Where(t => strong.Contains(t.FullName)))
        {
            TryAdd(table);
        }

        var weakAdded = 0;
        foreach (var table in ordered.Where(t => !strong.Contains(t.FullName)))
        {
            if (weakAdded >= _options.MaxWeakTables)
            {
                break;
            }

            if (TryAdd(table))
            {
                weakAdded++;
            }
        }

        // One hop along foreign keys from the best matches, into whatever room is left: the tables
        // a query on those would most likely need to join to.
        // Restricted to strongly-signalled tables: a passing prose mention of a very connected
        // table (Plan, Delivery) would otherwise drag in its whole neighbourhood every time.
        foreach (var table in selected.Where(t => strong.Contains(t.FullName)).Take(5).ToList())
        {
            foreach (var referenced in table.Referenced)
            {
                if (_byFullName.TryGetValue(referenced, out var parent))
                {
                    TryAdd(parent);
                }
            }
        }

        return selected;
    }

    // Full names ("schema.Table") of the known tables that appear right after a SQL keyword that
    // takes a table (FROM, JOIN, UPDATE, DELETE [FROM], INSERT INTO, TRUNCATE TABLE) in this text.
    // "[MoCS].[dbo].[Plan]", "dbo.Plan" and a bare "Plan" all resolve; a bare name that exists in
    // several schemas resolves to the dbo one, and is skipped if there is no dbo one. Words that
    // merely follow one of those keywords in prose ("from the system") aren't table names and are
    // dropped by the lookup.
    private HashSet<string> ExtractSqlTables(string text)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(text))
        {
            return found;
        }

        foreach (Match match in SqlTableReference().Matches(text))
        {
            var parts = match.Groups["name"].Value
                .Split('.')
                .Select(p => p.Trim('[', ']', '"', ' '))
                .Where(p => p.Length > 0)
                .ToArray();
            if (parts.Length == 0)
            {
                continue;
            }

            var tableName = parts[^1];
            TableBlock? hit = null;
            if (parts.Length >= 2 && _byFullName.TryGetValue($"{parts[^2]}.{tableName}", out var exact))
            {
                hit = exact;
            }
            else if (_byShortName.TryGetValue(tableName, out var candidates))
            {
                hit = candidates.Count == 1
                    ? candidates[0]
                    : candidates.FirstOrDefault(c => c.FullName.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase));
            }

            if (hit is not null)
            {
                found.Add(hit.FullName);
            }
        }

        return found;
    }

    private static int CountMatches(TableBlock table, string text)
    {
        if (text.Length == 0 || !text.Contains(table.ShortName, StringComparison.Ordinal))
        {
            return 0;
        }

        // Capped so one very repetitive text can't drown out everything else.
        return Math.Min(table.NamePattern.Matches(text).Count, 3);
    }

    private void LoadFullSchema(string[] lines)
    {
        var name = (string?)null;
        var block = new StringBuilder();

        void Flush()
        {
            if (name is null)
            {
                return;
            }

            var text = block.ToString().TrimEnd();
            var shortName = name[(name.IndexOf('.') + 1)..];
            var referenced = ForeignKeyTarget().Matches(text)
                .Select(m => m.Groups[1].Value)
                .Where(t => !t.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var pattern = new Regex(
                $"(?<![A-Za-z0-9_]){Regex.Escape(shortName)}(?![A-Za-z0-9_])",
                RegexOptions.CultureInvariant);

            var tableBlock = new TableBlock(name, shortName, text, referenced, pattern);
            _tables.Add(tableBlock);
            _byFullName[name] = tableBlock;
            if (!_byShortName.TryGetValue(shortName, out var sameShortName))
            {
                _byShortName[shortName] = sameShortName = [];
            }

            sameShortName.Add(tableBlock);
        }

        foreach (var line in lines)
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                Flush();
                var header = line[4..];
                var paren = header.IndexOf(" (", StringComparison.Ordinal);
                name = (paren >= 0 ? header[..paren] : header).Trim();
                block.Clear();
                block.AppendLine(line);
            }
            else if (name is not null)
            {
                block.AppendLine(line);
            }
        }

        Flush();
    }

    [GeneratedRegex(@"->\s+(\w+\.\w+)\.\w+")]
    private static partial Regex ForeignKeyTarget();

    [GeneratedRegex(
        @"\b(?:FROM|JOIN|UPDATE|INTO|DELETE(?:\s+FROM)?|TRUNCATE\s+TABLE)\s+(?<name>\[?\w+\]?(?:\.\[?\w+\]?){0,2})",
        RegexOptions.IgnoreCase)]
    private static partial Regex SqlTableReference();
}
