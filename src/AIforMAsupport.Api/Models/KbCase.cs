namespace AIforMAsupport.Api.Models;

// Mirrors the columns of Bridgestone_KB_cleaned.csv. Id is a real Mantis case number ("13525")
// for every row loaded from that CSV, or "AI-<n>" for a case the team added to the searchable
// pool via KbAdditions (see IKbAdditionStore/CompositeKbCaseRepository) - never a plain int, so
// the two kinds of id can share one column/type everywhere without collision.
public sealed record KbCase(
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
