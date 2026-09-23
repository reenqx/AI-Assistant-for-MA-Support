using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Supplies the candidate KB cases to feed the AI as context for a given question.
// Temporary hardcoded implementation for now; swapped for real SQL Full-Text keyword
// search once the search pipeline (Day 3-4 of the timeline) is built.
public interface IKbContextProvider
{
    IReadOnlyList<KbCase> GetContext(string question, int topN = 5);
}
