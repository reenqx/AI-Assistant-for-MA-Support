using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using AIforMAsupport.Shared;

namespace AIforMAsupport.Web.Services.Markdown;

// Minimal, dependency-free renderer for the markdown constructs the AI's answers actually use:
// headings, bullet/numbered lists, tables, code fences, inline code, bold. Every text fragment
// is HTML-escaped before any tag is added around it, so this can never turn a reflected question
// (e.g. containing "<script>") into live HTML - a full markdown engine that passes through raw
// HTML would risk exactly that on an assistant message that echoes user input.
public static partial class SimpleMarkdown
{
    // HtmlEncoder.Default only allow-lists Basic Latin, so every Thai character (this app's
    // primary content, not an edge case) gets emitted as a numeric entity like "&#3586;" - still
    // renders correctly in a browser, but roughly 7x the bytes on the wire, and it means no
    // downstream code can regex-match Thai text against the produced HTML string (DecisionTree/
    // CaseReference formatters need to). "<", ">", "&", quotes stay escaped regardless of the
    // allowed range - those are what actually matter for XSS, not which Unicode blocks pass
    // through unescaped.
    private static readonly HtmlEncoder ThaiSafeEncoder = HtmlEncoder.Create(new TextEncoderSettings(UnicodeRanges.All));

    // PromptBuilder (API project) instructs the AI to open any SQL it invents itself (not copied
    // from a real KnownScript) with this exact line. Detected here so that one code fence renders
    // as a visibly different "unverified" card instead of a plain code block - the marker line
    // itself is stripped from what's displayed, since the warning banner already says the same
    // thing in a way a user can't miss.
    private const string AiSqlMarker = "-- [AI-SQL:UNVERIFIED]";

    public static string ToHtml(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            if (line.TrimStart().StartsWith("```"))
            {
                i++;
                var codeLines = new List<string>();
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    codeLines.Add(lines[i]);
                    i++;
                }
                i++; // skip closing fence (or end of text if unterminated)

                var isAiSuggestedSql = codeLines.Count > 0 && codeLines[0].Trim() == AiSqlMarker;
                if (isAiSuggestedSql)
                {
                    codeLines.RemoveAt(0);
                }

                var code = ThaiSafeEncoder.Encode(string.Join('\n', codeLines));
                if (isAiSuggestedSql)
                {
                    // The user explicitly chose to allow running AI-authored SQL too (previously
                    // Copy-only forever), including write statements now (previously always
                    // blocked - the user explicitly confirmed this risk and that the target DB is
                    // a real separate copy/instance from production, not production itself). A
                    // write button renders as "ai-sql-run-btn-write" (danger-styled, see app.css)
                    // instead of the read-only green button, and console.js gates its click behind
                    // a native confirm() - on top of SqlScriptRunner re-checking AllowWrites and
                    // wrapping the execution in a real DB transaction server-side regardless.
                    sb.Append(AiSqlCardHtml(string.Join('\n', codeLines), code));
                }
                else
                {
                    sb.Append("<pre class=\"code-block\"><code>").Append(code).Append("</code></pre>");
                }
                continue;
            }

            var heading = HeadingRegex().Match(line);
            if (heading.Success)
            {
                sb.Append("<div class=\"md-heading\">").Append(Inline(heading.Groups[1].Value)).Append("</div>");
                i++;
                continue;
            }

            if (line.TrimStart().StartsWith('|') && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            {
                var headerCells = SplitTableRow(line);
                i += 2;
                sb.Append("<table class=\"md-table\"><thead><tr>");
                foreach (var h in headerCells)
                {
                    sb.Append("<th>").Append(Inline(h)).Append("</th>");
                }
                sb.Append("</tr></thead><tbody>");
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    sb.Append("<tr>");
                    foreach (var cell in SplitTableRow(lines[i]))
                    {
                        sb.Append("<td>").Append(Inline(cell)).Append("</td>");
                    }
                    sb.Append("</tr>");
                    i++;
                }
                sb.Append("</tbody></table>");
                continue;
            }

            if (UnorderedListRegex().IsMatch(line))
            {
                sb.Append("<ul class=\"md-list\">");
                while (i < lines.Length && UnorderedListRegex().Match(lines[i]) is { Success: true } m)
                {
                    sb.Append("<li>").Append(Inline(m.Groups[1].Value)).Append("</li>");
                    i++;
                }
                sb.Append("</ul>");
                continue;
            }

            if (OrderedListRegex().IsMatch(line))
            {
                sb.Append("<ol class=\"md-list\">");
                while (i < lines.Length && OrderedListRegex().Match(lines[i]) is { Success: true } m)
                {
                    sb.Append("<li>").Append(Inline(m.Groups[1].Value)).Append("</li>");
                    i++;
                }
                sb.Append("</ol>");
                continue;
            }

            if (line.Trim() is "---" or "***")
            {
                sb.Append("<hr class=\"md-hr\" />");
                i++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            var paragraphLines = new List<string>();
            while (i < lines.Length && !IsBlockStart(lines[i]))
            {
                paragraphLines.Add(lines[i]);
                i++;
            }
            sb.Append("<p>").Append(string.Join("<br />", paragraphLines.ConvertAll(Inline))).Append("</p>");
        }

        return sb.ToString();
    }

    // The card an SQL block in an answer becomes: banner, the code, a place for the run result and the
    // Run / Edit / Copy buttons. Used for blocks carrying the AI-SQL marker (above) and by
    // RunnableSqlFormatter for SQL that came without it - the AI sometimes forgets the marker, and a
    // block with no buttons at all leaves the person with nothing to run. encodedCode is the SQL
    // already HTML-encoded; rawSql is the same text unencoded, used only to classify it.
    public static string AiSqlCardHtml(string rawSql, string encodedCode)
    {
        var sb = new StringBuilder();
        var code = encodedCode;

            // The same rule the API applies before running anything (shared source file):
            // here it only decides how the card looks. A blocked statement (structure or
            // permission changes, procedure calls, ...) gets no Run button at all - the API
            // would refuse it anyway - but can still be copied or opened in the editor.
            var risk = SqlSafetyClassifier.ClassifyDisplayed(rawSql).Risk;
            var isWrite = risk == SqlRisk.Modifies;
            var isBlocked = risk == SqlRisk.Blocked;
            var unfilled = SqlPlaceholderCheck.HasUnfilled(rawSql);
            var runButtonHtml = isBlocked
                ? string.Empty
                : "<button type=\"button\" class=\"sql-run-btn ai-sql-run-btn" + (isWrite ? " ai-sql-run-btn-write" : "") + "\"" +
                  (unfilled ? " disabled title=\"ยังมีค่าที่ต้องระบุ (&lt;...&gt; หรือค่าว่าง) — บอกค่าให้ AI ในแชทก่อน แล้วรันได้เมื่อ SQL ใส่ค่าครบ\"" : "") + ">" +
                  (isWrite ? "Run live (modifies data)" : "Run live (read-only)") + "</button>" +
                  (unfilled ? "<span class=\"ai-sql-unfilled-hint\">ยังไม่ระบุค่า — รันจริงไม่ได้จนกว่า SQL จะใส่ค่าครบ</span>" : "");
            var banner = isBlocked
                ? "AI แต่งขึ้นเอง — SQL นี้ระบบไม่อนุญาตให้รันจากที่นี่ (มีคำสั่งสร้าง/แก้โครงสร้าง, รันโพรซีเดอร์ หรือสิทธิ์) ดูหรือแก้ไขได้ แต่ต้องคัดลอกไปรันเองใน SSMS"
                : isWrite
                    ? "AI แต่งขึ้นเอง — ยังไม่ผ่านการตรวจสอบ ไม่ใช่สคริปต์จริงในระบบ มีคำสั่งแก้ไขข้อมูล ตรวจสอบให้ดีมากๆ ก่อนกดรันจริง แก้คืนไม่ได้"
                    : "AI แต่งขึ้นเอง — ยังไม่ผ่านการตรวจสอบ ไม่ใช่สคริปต์จริงในระบบ ตรวจสอบให้ดีก่อนกดรันจริงหรือ Copy ไปใช้";

            sb.Append("<div class=\"ai-sql-block\">")
              .Append("<div class=\"ai-sql-banner\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M12 9v4\"></path><path d=\"M12 17h.01\"></path><path d=\"M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z\"></path></svg>")
              .Append(banner).Append("</div>")
              .Append("<pre class=\"code-block ai-sql-code\"><code>").Append(code).Append("</code></pre>")
              .Append("<div class=\"ai-sql-run-result\"></div>")
              .Append("<div class=\"ai-sql-copy-row\">").Append(runButtonHtml).Append("<button type=\"button\" class=\"ai-sql-edit-btn\">Edit</button><button type=\"button\" class=\"ai-sql-copy-btn\">Copy</button></div>")
              .Append("</div>");

        return sb.ToString();
    }

    private static string Inline(string raw)
    {
        var escaped = ThaiSafeEncoder.Encode(raw);
        var withInlineCode = InlineCodeRegex().Replace(escaped, m => $"<code>{m.Groups[1].Value}</code>");
        return BoldRegex().Replace(withInlineCode, m => $"<strong>{m.Groups[1].Value}</strong>");
    }

    private static bool IsBlockStart(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;
        if (line.TrimStart().StartsWith("```")) return true;
        if (HeadingRegex().IsMatch(line)) return true;
        if (UnorderedListRegex().IsMatch(line)) return true;
        if (OrderedListRegex().IsMatch(line)) return true;
        if (line.TrimStart().StartsWith('|')) return true;
        if (line.Trim() is "---" or "***") return true;
        return false;
    }

    private static bool IsTableSeparator(string line)
    {
        var t = line.Trim();
        if (!t.StartsWith('|') || !t.Contains('-'))
        {
            return false;
        }

        return t.Trim('|').Split('|').All(cell => cell.Trim().Trim(':').All(c => c == '-') && cell.Trim().Length > 0);
    }

    private static string[] SplitTableRow(string line) =>
        line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

    [GeneratedRegex(@"^\s{0,3}\#{1,6}\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s{0,3}[-*]\s+(.*)$")]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex(@"^\s{0,3}\d+\.\s+(.*)$")]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"`([^`\n]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"\*\*([^\*\n]+)\*\*")]
    private static partial Regex BoldRegex();
}
