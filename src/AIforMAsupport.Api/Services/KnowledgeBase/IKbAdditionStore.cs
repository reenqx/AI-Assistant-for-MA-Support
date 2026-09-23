using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Cases the team explicitly added to the searchable KB pool - see KbAdditionRecord. Read/write
// surface for the "เพิ่มเข้าคลังเคส" feature (POST /api/kbadditions from /SavedCases).
public interface IKbAdditionStore
{
    // Cheap, synchronous, shaped exactly like a CSV-loaded case - this is what
    // CompositeKbCaseRepository.GetAll() calls on every search, so it stays off the async/await
    // path entirely (same reasoning as CsvKbCaseRepository's own synchronous load).
    IReadOnlyList<KbCase> GetAll();

    // Full records (with the bookkeeping fields GetAll()'s KbCase shape has no room for) for the
    // /SavedCases page: which saved cases are already added, when, and their AI-<n> id.
    Task<IReadOnlyList<KbAdditionRecord>> ListAsync(CancellationToken cancellationToken = default);

    // Idempotent on sourceSavedCaseId: adding the same saved case twice returns the existing
    // record instead of creating a duplicate KB entry, so a double-click or a retried request
    // can't leave two near-identical cases in the pool.
    Task<KbAdditionRecord> AddAsync(
        string bsModule,
        string subCategory,
        string caseType,
        string summary,
        string kbContent,
        int? sourceSavedCaseId,
        CancellationToken cancellationToken = default);

    // True if a row with this id existed and was removed; false if there was nothing to remove.
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
