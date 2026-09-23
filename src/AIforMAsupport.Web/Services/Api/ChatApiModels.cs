namespace AIforMAsupport.Web.Services.Api;

// Mirrors AIforMAsupport.Api.Models.ChatRequest/ChatResponse. Kept as a separate copy rather
// than a shared project reference since the two are small and the API is the only consumer -
// not worth the extra project/deployment unit for a 2-week hackathon build.
public sealed record ChatApiRequest(string Question, Guid? ConversationId);

public sealed record ChatApiResponse(
    Guid ConversationId,
    string Answer,
    IReadOnlyList<string> ReferencedCaseIds,
    bool IsRawCaseLookup,
    IReadOnlyList<KnownScriptDto> RelevantScripts);

// Mirrors AIforMAsupport.Api.Models.KbCase. Id is a real Mantis case number ("13525") or a
// team-added one ("AI-1") - see KbAdditionRecord on the Api side.
public sealed record KbCaseSummary(
    string Id,
    string BsModule,
    string SubCategory,
    string CaseType,
    string Summary,
    string Description,
    string StepsToReproduce,
    string AdditionalInformation,
    string Notes,
    string KbContent);

// Mirrors AIforMAsupport.Api.Services.KnownScripts.KnownScript.
public sealed record KnownScriptDto(string Name, string Description, string SqlContent);

// Mirrors AIforMAsupport.Api.Models.ChatSearchResponse - the fast, no-AI-call preview.
public sealed record ChatApiSearchResponse(
    Guid ConversationId,
    IReadOnlyList<KbCaseSummary> Cases,
    bool IsFinal,
    string? FinalAnswer,
    IReadOnlyList<KnownScriptDto> RelevantScripts);

// Mirrors AIforMAsupport.Api.Services.History.ConversationSummary/HistoryTurn.
public sealed record ConversationSummaryDto(Guid ConversationId, string FirstQuestion, DateTime LastActivityUtc, int TurnCount);

public sealed record HistoryTurnDto(int TurnIndex, string Question, string Answer, IReadOnlyList<string> ReferencedCaseIds, DateTime CreatedAtUtc);

// Mirrors AIforMAsupport.Api.Models.SqlRunModels - the live, read-only SQL Studio "run" call.
// Exactly one of ScriptName/SqlText should be set - SqlText covers AI-authored SQL, which the
// user explicitly chose to allow running (still read-only only - see SqlScriptRunner).
public sealed record SqlRunApiRequest(string? ScriptName, string? SqlText, IReadOnlyDictionary<string, string> Parameters);

public sealed record SqlResultTableDto(string? Label, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows);

public sealed record SqlRunApiResponse(IReadOnlyList<SqlResultTableDto> Tables);

// The API returns 400 for a rejected request (unknown/write script, feature disabled) and 502
// for a real DB failure - both carry a Thai-language "error" message worth surfacing as-is
// rather than losing it behind a generic EnsureSuccessStatusCode exception.
public sealed record SqlRunApiResult(bool Success, SqlRunApiResponse? Response, string? ErrorMessage);

// Mirrors AIforMAsupport.Api.Models.SavedCase / SavedCasesController.SaveCaseRequest.
public sealed record SaveCaseApiRequest(
    Guid ConversationId,
    string Summary,
    IReadOnlyList<string> ReferencedCaseIds,
    IReadOnlyList<string> KnownScripts);

public sealed record SavedCaseDto(
    int Id,
    Guid ConversationId,
    string Summary,
    IReadOnlyList<string> ReferencedCaseIds,
    IReadOnlyList<string> KnownScripts,
    DateTime SavedAtUtc,
    string? MantisIssueId);

// Mirrors AIforMAsupport.Api.Models.KbAdditionRecord.
public sealed record KbAdditionDto(
    string Id,
    string BsModule,
    string SubCategory,
    string CaseType,
    string Summary,
    string KbContent,
    int? SourceSavedCaseId,
    DateTime AddedAtUtc);
