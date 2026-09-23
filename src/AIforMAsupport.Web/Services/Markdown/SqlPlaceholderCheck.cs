using System.Text.RegularExpressions;

namespace AIforMAsupport.Web.Services.Markdown;

// True when a SQL block still has a value the user must fill in before it can safely run: a
// "<placeholder>" token, or a DECLARE whose value is empty. Comments are ignored so a note like
// "-- แทน <วันที่ยืนยัน> ด้วยค่าจริง" doesn't count. Used to render the "รันจริง" button disabled
// (SqlScriptRunner on the API side re-checks this independently, never trusting the UI).
public static partial class SqlPlaceholderCheck
{
    public static bool HasUnfilled(string sql)
    {
        var code = LineComment().Replace(BlockComment().Replace(sql, " "), string.Empty);
        return AnglePlaceholder().IsMatch(code) || EmptyDeclare().IsMatch(code);
    }

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"<\p{L}[^<>\r\n]{0,60}?>")]
    private static partial Regex AnglePlaceholder();

    [GeneratedRegex(@"^\s*DECLARE\s+@\w+\s+\w+(?:\([^)]*\))?\s*=\s*''\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex EmptyDeclare();
}
