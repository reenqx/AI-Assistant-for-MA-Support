namespace AIforMAsupport.Api.Services.History;

public sealed record ConversationSummary(
    Guid ConversationId,
    string FirstQuestion,
    DateTime LastActivityUtc,
    int TurnCount);

public sealed record HistoryTurn(
    int TurnIndex,
    string Question,
    string Answer,
    IReadOnlyList<string> ReferencedCaseIds,
    DateTime CreatedAtUtc);
