namespace AIforMAsupport.Api.Services.Conversation;

// Backs the follow-up shortcuts (ร่างตอบลูกค้า / บันทึกเคส / เคสซ้ำ), which
// need "the previous question" the way a human support engineer would remember it. Each headless
// claude CLI call is a fresh process with no memory of prior turns, so the API keeps that memory
// server-side, keyed by a conversation id the client round-trips on each call.
public interface IConversationStore
{
    ConversationTurn? GetLastTurn(Guid conversationId);

    void SetLastTurn(Guid conversationId, ConversationTurn turn);

    // Full names ("schema.Table") of the tables whose definitions went into the prompt on this
    // conversation's most recent AI turn. The next turn's table selection favours them (see
    // FileDbSchemaProvider), so the tables used while checking a problem are still in front of the
    // AI when it writes the fix. Empty for a conversation with no AI turn yet.
    IReadOnlyList<string> GetSchemaTables(Guid conversationId);

    void SetSchemaTables(Guid conversationId, IReadOnlyList<string> tables);
}
