namespace AIforMAsupport.Api.Services.Sql;

public sealed class DbSchemaOptions
{
    public const string SectionName = "DbSchema";

    // Small hand-written notes (naming rules, what key columns mean) - always sent in full.
    // Path is relative to the API project's working directory (repo root's data/ folder, two levels up).
    public string FilePath { get; set; } = "../../data/MoCS-Schema-Reference.md";

    // Full generated schema (every table/view, all columns, PK/FK) - never sent whole; only the
    // tables relevant to the current question are picked out of it per request.
    public string FullFilePath { get; set; } = "../../data/MoCS-Schema-Full.md";

    // Caps on how much of the full schema goes into one prompt, so a question that happens to
    // mention many table names can't blow up prompt size (and response time).
    // 0 = don't send any per-question tables at all.
    public int MaxTables { get; set; } = 8;
    public int MaxChars { get; set; } = 7000;

    // Of those, how many may be tables that were only mentioned in passing (no script or case SQL
    // behind them).
    public int MaxWeakTables { get; set; } = 3;
}
