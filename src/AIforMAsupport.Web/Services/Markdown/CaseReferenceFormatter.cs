using System.Text.RegularExpressions;

namespace AIforMAsupport.Web.Services.Markdown;

// Turns every "#1234"- or "#AI-1"-shaped case reference in an already-rendered answer into a
// clickable pill (client-side JS wires data-open-case to the Reference Case Drawer) - "ทุกครั้งที่มี
// การอ้าง (เคส #xxxx) ... ให้เรนเดอร์เป็น Clickable Tag/Pill" per the console design spec. The
// "AI-<n>" form is a team-added KB entry (see KbAdditionRecord), not a real Mantis case number.
//
// Runs AFTER SimpleMarkdown.ToHtml, so the input is already fully HTML-escaped and any "#" that
// was part of a markdown heading has already been consumed into a <div class="md-heading"> by
// that point - a literal "#" surviving in the text at this stage is always a real case reference,
// never markup syntax, so matching plain "#<id>" text here is safe.
public static partial class CaseReferenceFormatter
{
    [GeneratedRegex(@"#(AI-\d+|\d{3,6})\b")]
    private static partial Regex CaseRefPattern();

    public static string Enhance(string html) =>
        CaseRefPattern().Replace(html, static m =>
            $"<button type=\"button\" class=\"case-pill\" data-open-case=\"{m.Groups[1].Value}\">#{m.Groups[1].Value}</button>");
}
