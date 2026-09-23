using System.Text.RegularExpressions;
using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnownScripts;

// Correlates known scripts to the current question by name: KB cases usually only ever mention
// a script's name (e.g. "3.MA_Reset_TireChecker_Finish_Status"), never its actual SQL - so if
// that name shows up in the matched case context or the question itself, the script is almost
// certainly relevant and its real SQL is worth including.
//
// Tier 1 (exact match key) is the original, strictest behavior - kept as the fast path since
// it's the strongest possible signal. In practice, though, real case text almost never quotes a
// script's filename verbatim; it describes the same topic in prose ("Reset TirecheckerScanlog",
// "ถอยสถานะสแกนยาง...") - so Tier 2 falls back to requiring ALL of the script name's own
// significant words (splitting compound/underscore-joined names apart, and dropping generic verbs
// that would false-positive against unrelated scripts) to show up instead of the whole name intact.
// Requiring only "most" of them was tried and measurably over-matched: a case merely mentioning
// "Tirechecker" (e.g. an unrelated Add-BSJCode case) satisfied "half of {Tirechecker, Scan}"
// on its own and pulled in the scan-log scripts. Requiring every remaining word keeps the signal
// topical without needing the exact compound spelling/ordering.
public static partial class KnownScriptMatcher
{
    // Deliberately narrow: only words that would appear in the topic-word set of MULTIPLE
    // unrelated hypothetical scripts and so carry no distinguishing power on their own ("MA" is
    // the app prefix on every script; "Remove"/"Reset"/"Query" are generic verbs; "Log" is a
    // generic IT noun). "Finish"/"Status" were tried here too but had to come back out - without
    // them, script #3's only remaining significant words were "Tire"+"Checker", both of which are
    // trivially satisfied by ANY text containing the single word "Tirechecker" (they're substrings
    // of it), which measurably over-matched (an unrelated Add-BSJCode case, which just happens to
    // mention "Tirechecker 2D" for a different sub-task, pulled this script in).
    private static readonly HashSet<string> GenericWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "MA", "Remove", "Reset", "Log", "Query", "Some", "Code", "GT",
    };

    public static IReadOnlyList<KnownScript> FindRelevant(
        IReadOnlyList<KnownScript> allScripts,
        IReadOnlyList<KbCase> context,
        string question)
    {
        if (allScripts.Count == 0)
        {
            return [];
        }

        var haystack = string.Join(' ', context.Select(c => c.KbContent).Append(question));

        return allScripts.Where(s => IsRelevant(haystack, s.Name)).ToList();
    }

    private static bool IsRelevant(string haystack, string scriptName)
    {
        var key = MatchKey(scriptName);
        if (haystack.Contains(key, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var words = SignificantWords(key);
        if (words.Count == 0)
        {
            return false;
        }

        return words.All(w => haystack.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    // "TirecheckerScanLog" -> "Tirechecker", "Scan", "Log"; "MA_Reset_TireChecker_Finish_Status"
    // -> "MA", "Reset", "Tire", "Checker", "Finish", "Status" - splits both on "_" and internal
    // camelCase boundaries, since script authors were inconsistent about which they used. Generic
    // verbs/nouns shared across many hypothetical scripts are dropped so this stays a topical
    // signal ("Tirechecker") rather than firing on any script whenever "Reset" appears.
    private static List<string> SignificantWords(string key) =>
        CamelOrUnderscoreSplit().Split(key)
            .Where(w => w.Length > 2 && !GenericWords.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // "1.MA_Remove_TirecheckerScanLog" -> "MA_Remove_TirecheckerScanLog"
    // "2.MA_Remove_TirecheckerScanLog(some gtcode)" -> "MA_Remove_TirecheckerScanLog" (same base
    // name as #1 on purpose - when that name is mentioned, both the full-delete and the
    // GTCode-scoped variant are useful for the AI to see side by side).
    private static string MatchKey(string name)
    {
        var withoutPrefix = name;
        var dotIndex = name.IndexOf('.');
        if (dotIndex is > 0 and <= 2 && name[..dotIndex].All(char.IsDigit))
        {
            withoutPrefix = name[(dotIndex + 1)..];
        }

        var parenIndex = withoutPrefix.IndexOf('(');
        return parenIndex > 0 ? withoutPrefix[..parenIndex] : withoutPrefix;
    }

    [GeneratedRegex(@"_|(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelOrUnderscoreSplit();
}
