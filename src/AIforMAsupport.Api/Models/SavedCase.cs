namespace AIforMAsupport.Api.Models;

// A "บันทึกเคส" a person actually reviewed and confirmed - see ISavedCaseStore. Distinct from
// IConversationHistoryStore's turn-by-turn audit log, which records every question/answer
// regardless of whether anyone acted on it.
public sealed record SavedCase(
    int Id,
    Guid ConversationId,
    string Summary,
    IReadOnlyList<string> ReferencedCaseIds,
    IReadOnlyList<string> KnownScripts,
    DateTime SavedAtUtc,
    // Filled in once this case is actually filed/updated in Mantis - not this round (see
    // CLAUDE.md's scope decision on two-way Mantis integration). Left in the schema now so a
    // later phase can add that without a migration.
    string? MantisIssueId);
