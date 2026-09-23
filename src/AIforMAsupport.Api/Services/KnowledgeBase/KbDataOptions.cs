namespace AIforMAsupport.Api.Services.KnowledgeBase;

public sealed class KbDataOptions
{
    public const string SectionName = "KbData";

    // Path is relative to the API project's working directory (repo root's data/ folder, two levels up).
    public string CsvFilePath { get; set; } = "../../data/Bridgestone_KB_cleaned.csv";
}
