using System.Net;
using System.Text.RegularExpressions;
using AIforMAsupport.Shared;
using AIforMAsupport.Web.Pages;

namespace AIforMAsupport.Web.Services.Markdown;

// Matches a plain fenced code block in the AI's answer (a real Known Script quoted verbatim, or
// a trimmed excerpt of one - see PromptBuilder, which gives the AI the real script text to quote
// from and KnownScriptMatcher for how RelevantScripts gets computed) to one of this turn's
// RelevantScripts, purely by content: after stripping whitespace and DECLARE lines from both
// sides, if the block's own SQL is a substring of the real script's SQL, it's either the full
// script or an excerpt of it, and a "รันจริง" button can safely run the real thing server-side
// (SqlController re-derives the canonical text from disk by name - it never trusts what's
// displayed). AI-authored blocks render with an extra "ai-sql-code" class (see SimpleMarkdown)
// and so never match the exact "code-block" class this looks for.
//
// Write scripts (DELETE/UPDATE/INSERT) get a button too now - the user explicitly chose to allow
// this, understanding the risk, having confirmed the target DB (MoCS_dev) is a real separate
// copy/instance from production - rendered as "chat-sql-run-btn-write" (danger-styled, see
// app.css) instead of the read-only green button, and console.js gates its click behind a native
// confirm() on top of SqlScriptRunner independently re-checking AllowWrites and wrapping the
// execution in a real DB transaction server-side regardless.
public static partial class RunnableSqlFormatter
{
    public static string Enhance(string html, IReadOnlyList<IndexModel.KnownScriptPreview> scripts)
    {
        return CodeBlockRegex().Replace(html, match =>
        {
            var blockText = WebUtility.HtmlDecode(match.Groups["code"].Value);
            var normalizedBlock = Normalize(blockText);
            if (normalizedBlock.Length < 20)
            {
                return match.Value;
            }

            var matchedScript = scripts.FirstOrDefault(s => Normalize(s.SqlContent).Contains(normalizedBlock));
            if (matchedScript is null)
            {
                // Not (part of) a real Known Script. If it is SQL anyway - the AI forgot the
                // "-- [AI-SQL:UNVERIFIED]" marker, most often on the second step of a check-then-fix
                // answer - it still gets the same card (warning, Run / Edit / Copy) instead of a
                // dead code block: a person with a fix in front of them and no way to run it is the
                // worse outcome, and every run is classified and re-checked by the API regardless.
                return LooksLikeSql(blockText)
                    ? SimpleMarkdown.AiSqlCardHtml(blockText, match.Groups["code"].Value)
                    : match.Value;
            }

            // Same rule the API applies before running anything (shared source file) - here it
            // only labels the button. A real Known Script is never blocked; if one ever were, no
            // Run button is attached (the API would refuse it anyway).
            var risk = SqlSafetyClassifier.ClassifyDisplayed(matchedScript.SqlContent).Risk;
            if (risk == SqlRisk.Blocked)
            {
                return match.Value;
            }

            var isWrite = risk == SqlRisk.Modifies;
            var unfilled = SqlPlaceholderCheck.HasUnfilled(blockText);
            var name = WebUtility.HtmlEncode(matchedScript.Name);
            var btnClass = isWrite ? "sql-run-btn chat-sql-run-btn chat-sql-run-btn-write" : "sql-run-btn chat-sql-run-btn";
            var btnLabel = isWrite ? "Run live (modifies data) — " + name : "Run live (read-only) — " + name;
            return match.Value +
                "<div class=\"chat-sql-run-block\">" +
                "<button type=\"button\" class=\"" + btnClass + "\" data-script-name=\"" + name + "\" data-write=\"" + (isWrite ? "true" : "false") + "\"" +
                (unfilled ? " disabled title=\"ยังมีค่าที่ต้องระบุ (ค่าว่างหรือ &lt;...&gt;) — บอกค่าให้ AI ในแชทก่อน แล้วรันได้เมื่อ SQL ใส่ค่าครบ\"" : "") + ">" +
                btnLabel + "</button>" +
                "<button type=\"button\" class=\"sql-edit-secondary chat-sql-edit-btn\" data-script-name=\"" + name + "\">Edit</button>" +
                (unfilled ? "<span class=\"ai-sql-unfilled-hint\">ยังไม่ระบุค่า — รันจริงไม่ได้จนกว่า SQL จะใส่ค่าครบ</span>" : "") +
                "<div class=\"sql-run-results-inline\"></div>" +
                "</div>";
        });
    }

    // True when the first thing in the block, past any comment lines, is a SQL statement keyword.
    // Deliberately about the START of the block only: a code block that merely mentions SQL words
    // somewhere (a log excerpt, a config value) is not turned into a runnable card.
    private static bool LooksLikeSql(string blockText)
    {
        if (blockText.Length < 12)
        {
            return false;
        }

        foreach (var rawLine in blockText.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("--"))
            {
                continue;
            }

            return SqlStatementStart().IsMatch(line);
        }

        return false;
    }

    [GeneratedRegex(@"^(?:SELECT|WITH|UPDATE|DELETE|INSERT|MERGE|DECLARE|BEGIN\s+TRAN|SET\s+@|;WITH)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SqlStatementStart();

    // DECLARE lines carry whatever value the AI filled in (or an empty default) - only the query
    // body itself needs to match, so they're stripped before comparing. Whitespace differences
    // (line wrap, indentation) between the prompt's copy and the model's quoted copy are stripped
    // too rather than compared. The raw KnownScript.SqlContent on disk still has the hardcoded
    // "[MoCS]." three-part qualifier (see SqlScriptRunner/console.js, which both strip it too, in
    // their own separate contexts) - but the schema-reference instruction tells the model to
    // write two-part names, and it applies that consistently even when quoting the real script
    // verbatim, not just in SQL it invents. Without stripping "[MoCS]." here too, a faithful
    // "[dbo].[Foo]" quote would never substring-match the real script's "[MoCS].[dbo].[Foo]" and
    // this would silently never find a match.
    private static string Normalize(string sql)
    {
        // Known Scripts carry explanatory comments now, and the model may quote a script with or
        // without them - comments are stripped from both sides so they never affect the match.
        var withoutComments = BlockComment().Replace(LineComment().Replace(sql, string.Empty), string.Empty);
        var withoutDeclares = DeclareLine().Replace(withoutComments, string.Empty);
        var withoutMoCsQualifier = MoCsDatabaseQualifier().Replace(withoutDeclares, string.Empty);
        return WhitespaceRegex().Replace(withoutMoCsQualifier, string.Empty);
    }

    [GeneratedRegex("<pre class=\"code-block\"><code>(?<code>.*?)</code></pre>", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"^\s*DECLARE\s+@\w+\s+\w+(?:\([^)]*\))?\s*=\s*'[^']*'\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex DeclareLine();

    [GeneratedRegex(@"\[MoCS\]\.(?=\[)", RegexOptions.IgnoreCase)]
    private static partial Regex MoCsDatabaseQualifier();

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
