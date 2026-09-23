using System.Collections.Concurrent;

namespace AIforMAsupport.Api.Services.Conversation;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, ConversationTurn> _turns = new();

    public ConversationTurn? GetLastTurn(Guid conversationId) =>
        _turns.TryGetValue(conversationId, out var turn) ? turn : null;

    public void SetLastTurn(Guid conversationId, ConversationTurn turn) =>
        _turns[conversationId] = turn;

    private readonly ConcurrentDictionary<Guid, IReadOnlyList<string>> _schemaTables = new();

    public IReadOnlyList<string> GetSchemaTables(Guid conversationId) =>
        _schemaTables.TryGetValue(conversationId, out var tables) ? tables : [];

    public void SetSchemaTables(Guid conversationId, IReadOnlyList<string> tables) =>
        _schemaTables[conversationId] = tables;
}
