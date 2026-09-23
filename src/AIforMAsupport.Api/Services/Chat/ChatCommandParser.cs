namespace AIforMAsupport.Api.Services.Chat;

public enum ChatCommandKind
{
    // Plain question -> fresh keyword search + answer.
    Question,

    // "#<id>" -> raw case lookup, bypasses the AI entirely.
    CaseLookup,

    // "ร่างตอบลูกค้า" -> rewrites the previous answer as a customer-safe message (3.7 / command table).
    DraftCustomerReply,

    // "บันทึกเคส" -> summarizes the previous answer into a Mantis-postable note.
    SaveCaseNote,

    // "เคสซ้ำ" -> broader search to find how often this symptom recurs and across what Id range.
    DuplicateCheck,
}

public sealed record ParsedCommand(ChatCommandKind Kind, string? CaseId, string CommandLabel, string? RemainderText);

public static class ChatCommandParser
{
    private static readonly Dictionary<string, ChatCommandKind> FollowUpKeywords = new()
    {
        ["ร่างตอบลูกค้า"] = ChatCommandKind.DraftCustomerReply,
        ["บันทึกเคส"] = ChatCommandKind.SaveCaseNote,
        ["เคสซ้ำ"] = ChatCommandKind.DuplicateCheck,
    };

    public static ParsedCommand Parse(string input)
    {
        var trimmed = input.Trim();

        if (trimmed.StartsWith('#'))
        {
            // Accepts both a real Mantis case number ("#13525") and a team-added one ("#AI-1") -
            // KbCaseRepository.GetAll() carries both kinds under the same string Id. Kept as strict
            // a shape check as the old int.TryParse was, just widened to allow the "AI-<digits>"
            // form too, so a message that merely starts with "#" for some unrelated reason still
            // falls through to being treated as a plain question instead of a failed case lookup.
            var idPart = trimmed[1..].Trim();
            if (IsCaseIdShape(idPart))
            {
                return new ParsedCommand(ChatCommandKind.CaseLookup, idPart, trimmed, null);
            }
        }

        foreach (var (keyword, kind) in FollowUpKeywords)
        {
            if (trimmed.Equals(keyword, StringComparison.Ordinal))
            {
                return new ParsedCommand(kind, null, keyword, null);
            }

            if (trimmed.StartsWith(keyword, StringComparison.Ordinal))
            {
                var remainder = trimmed[keyword.Length..].Trim();
                if (remainder.Length > 0)
                {
                    return new ParsedCommand(kind, null, keyword, remainder);
                }
            }
        }

        return new ParsedCommand(ChatCommandKind.Question, null, trimmed, null);
    }

    // "13525" (a real Mantis case) or "AI-1" (a team-added one) - see KbAdditionRecord.
    private static bool IsCaseIdShape(string idPart)
    {
        if (idPart.StartsWith("AI-", StringComparison.Ordinal))
        {
            return idPart.Length > 3 && idPart[3..].All(char.IsAsciiDigit);
        }

        return idPart.Length > 0 && idPart.All(char.IsAsciiDigit);
    }
}
