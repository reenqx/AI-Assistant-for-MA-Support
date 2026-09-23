using AIforMAsupport.Api.Services.KnownScripts;

namespace AIforMAsupport.Api.Models;

// ConversationId is null on the first message of a conversation; the server generates one and
// returns it, and the client must echo it back on later calls for follow-up shortcuts
// (ร่างตอบลูกค้า / บันทึกเคส / เคสซ้ำ) to have something to follow up on.
public sealed record ChatRequest(string Question, Guid? ConversationId);

public sealed record ChatResponse(
    Guid ConversationId,
    string Answer,
    IReadOnlyList<string> ReferencedCaseIds,
    bool IsRawCaseLookup,
    IReadOnlyList<KnownScript> RelevantScripts);

// Fast path (server-side keyword search only, no AI call - typically <1s): lets the UI show
// candidate cases immediately instead of a blank spinner for however long the AI call takes.
// IsFinal is true when there's nothing left to do (a raw "#<id>" lookup, a not-found, or a
// friendly error) - the client should skip calling POST /api/chat entirely in that case.
public sealed record ChatSearchResponse(
    Guid ConversationId,
    IReadOnlyList<KbCase> Cases,
    bool IsFinal,
    string? FinalAnswer,
    IReadOnlyList<KnownScript> RelevantScripts);
