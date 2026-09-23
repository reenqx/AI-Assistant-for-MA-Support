namespace AIforMAsupport.Web.Services.Api;

public interface ICaseCopilotApiClient
{
    // Fast path: KB keyword search only, no AI call (typically <1s).
    Task<ChatApiSearchResponse> SearchAsync(string question, Guid? conversationId, CancellationToken cancellationToken = default);

    // Slow path: full AI-synthesized answer.
    Task<ChatApiResponse> AskAsync(string question, Guid? conversationId, CancellationToken cancellationToken = default);

    // Same full answer, but onTextDelta is called with each piece of it as the model writes it.
    // Returns the complete response once the answer is finished.
    Task<ChatApiResponse> AskStreamAsync(string question, Guid? conversationId, Func<string, Task> onTextDelta, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryDto>> ListHistoryAsync(int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryTurnDto>?> GetHistoryConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);

    // Live, read-only execution against the real database - either a whitelisted KnownScript by
    // name, or (since the user explicitly chose to allow it) raw AI-authored SQL text. Exactly
    // one of scriptName/sqlText should be non-null.
    Task<SqlRunApiResult> RunSqlAsync(string? scriptName, string? sqlText, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default);

    // Persists a "บันทึกเคส" the person reviewed (and possibly edited) in the save dialog - see
    // SavedCasesController. summary is saved verbatim, exactly as they finalized it.
    Task<SavedCaseDto> SaveCaseAsync(
        Guid conversationId,
        string summary,
        IReadOnlyList<string> referencedCaseIds,
        IReadOnlyList<string> knownScripts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedCaseDto>> ListSavedCasesAsync(int limit, CancellationToken cancellationToken = default);

    // "เพิ่มเข้าคลังเคส" - promotes a reviewed SavedCase into the searchable KB pool. sourceSavedCaseId
    // makes the call idempotent server-side (see IKbAdditionStore.AddAsync): calling it twice for
    // the same saved case returns the existing KB entry instead of creating a duplicate.
    Task<KbAdditionDto> AddKbAdditionAsync(
        string? bsModule,
        string? subCategory,
        string? caseType,
        string summary,
        string? kbContent,
        int? sourceSavedCaseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KbAdditionDto>> ListKbAdditionsAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteKbAdditionAsync(string id, CancellationToken cancellationToken = default);
}
