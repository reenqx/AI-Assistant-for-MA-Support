namespace AIforMAsupport.Api.Models;

// A case the team explicitly added to the searchable KB pool (via the "เพิ่มเข้าคลังเคส" button on
// /SavedCases), as opposed to one of the 2,180 real cases in Bridgestone_KB_cleaned.csv. Carries a
// couple of bookkeeping fields (SourceSavedCaseId, AddedAtUtc) that KbCase - which mirrors the CSV
// columns exactly and is what the search pipeline actually consumes - has no room for.
public sealed record KbAdditionRecord(
    string Id,
    string BsModule,
    string SubCategory,
    string CaseType,
    string Summary,
    string KbContent,
    int? SourceSavedCaseId,
    DateTime AddedAtUtc);
