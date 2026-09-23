namespace AIforMAsupport.Api.Services.History;

// Durable audit trail: every completed turn (question, answer, cited case IDs), independent of
// IConversationStore.Services.Conversation (which only ever remembers the single most recent
// turn per conversation, in memory, purely to let follow-up messages reuse that turn's context -
// see ChatController.ResolveQuestion). This store is append-only and survives API restarts.
public interface IConversationHistoryStore
{
    Task AppendAsync(
        Guid conversationId,
        string question,
        string answer,
        IReadOnlyList<string> referencedCaseIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryTurn>> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
