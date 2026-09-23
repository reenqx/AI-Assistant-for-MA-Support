using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Ranks cases by how many distinct keywords from the question appear in their searchable text.
// KB_Content already carries every field merged into one string (per the system prompt's own
// description of that column), so it's the primary match target; Summary/Sub_Category are
// added on top since Sub_Category is only ~22% filled but, when present, uses cleaner wording
// than the free-text KB_Content (e.g. "Tirechecker" vs. a misspelled "tirescheckder").
public sealed class KeywordSearchKbContextProvider : IKbContextProvider
{
    private static readonly char[] Delimiters = [' ', '\t', '\r', '\n', ',', '.', '?', '!', '"', '(', ')'];

    private readonly IKbCaseRepository _repository;

    public KeywordSearchKbContextProvider(IKbCaseRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<KbCase> GetContext(string question, int topN = 5)
    {
        var keywords = Tokenize(question);
        if (keywords.Count == 0)
        {
            return [];
        }

        return _repository.GetAll()
            .Select(c => (Case: c, Score: Score(c, keywords)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => IdSortKey(x.Case.Id))
            .Take(topN)
            .Select(x => x.Case)
            .ToList();
    }

    // Score() gives a real Mantis case and a team-added "AI-<n>" one equal weight on purpose (see
    // CLAUDE.md) - this is only the tie-break for when two cases score identically, and it exists
    // purely to keep results deterministic, not to prefer one kind of case over the other. Id used
    // to be an int and the old code just did ThenByDescending(Id); a case's numeric id (its Mantis
    // number, or the "<n>" in "AI-<n>") is still the more recently added/higher one winning ties,
    // same as before - just parsed back out of the string now that Id itself can't be compared as
    // a number directly.
    private static long IdSortKey(string id)
    {
        var digits = id.StartsWith("AI-", StringComparison.Ordinal) ? id[3..] : id;
        return long.TryParse(digits, out var value) ? value : 0;
    }

    private static List<string> Tokenize(string text) =>
        text.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int Score(KbCase c, List<string> keywords)
    {
        var score = 0;
        foreach (var keyword in keywords)
        {
            if (c.KbContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }

            if (c.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }

            if (c.SubCategory.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        return score;
    }
}
