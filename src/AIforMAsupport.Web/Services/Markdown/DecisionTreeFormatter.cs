using System.Text.RegularExpressions;

namespace AIforMAsupport.Web.Services.Markdown;

// Phase 1 of the Decision Tree tab: the AI has no structured step/branch output today (see
// Project_Instruction_MA_Bridgestone_v2.md §3.3 - it's a natural-language formatting convention,
// not a schema), so there is nothing to parse into real clickable nodes yet. This just makes the
// existing prose *read* more like a decision tree, by lightly re-styling the "ขั้น N" / "เจอ →" /
// "ไม่เจอ →" tokens the system prompt already asks the model to use.
//
// Runs AFTER SimpleMarkdown.ToHtml, so the input is already fully HTML-escaped - every pattern
// here matches plain Thai/ASCII text with no "<" or ">" in it, so wrapping a match in a new tag
// can never merge with or break an existing tag boundary. A reply that doesn't happen to use
// this phrasing (e.g. a short answer) passes through unchanged.
public static partial class DecisionTreeFormatter
{
    [GeneratedRegex(@"ขั้น\s*\d+")]
    private static partial Regex StepPattern();

    // Negative lookbehind so "ไม่เจอ →" (which contains the substring "เจอ →") is never also
    // matched by the "found" pattern - each token gets wrapped exactly once.
    [GeneratedRegex(@"(?<!ไม่)เจอ\s*→")]
    private static partial Regex FoundBranchPattern();

    [GeneratedRegex(@"ไม่เจอ\s*→")]
    private static partial Regex NotFoundBranchPattern();

    public static string Enhance(string html)
    {
        var withSteps = StepPattern().Replace(html, static m => $"<strong class=\"tree-step-num\">{m.Value}</strong>");
        var withNotFound = NotFoundBranchPattern().Replace(withSteps, static m => $"<span class=\"tree-branch-no\">{m.Value}</span>");
        var withFound = FoundBranchPattern().Replace(withNotFound, static m => $"<span class=\"tree-branch-yes\">{m.Value}</span>");

        return withFound;
    }
}
