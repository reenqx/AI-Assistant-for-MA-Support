using CsvHelper.Configuration.Attributes;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Maps 1:1 to the header row of Bridgestone_KB_cleaned.csv.
public sealed class KbCaseCsvRecord
{
    [Name("Id")]
    public string Id { get; set; } = string.Empty;

    [Name("BS_Module")]
    public string BsModule { get; set; } = string.Empty;

    [Name("Sub_Category")]
    public string SubCategory { get; set; } = string.Empty;

    [Name("Case_Type")]
    public string CaseType { get; set; } = string.Empty;

    [Name("Summary")]
    public string Summary { get; set; } = string.Empty;

    [Name("Description")]
    public string Description { get; set; } = string.Empty;

    [Name("Steps_To_Reproduce")]
    public string StepsToReproduce { get; set; } = string.Empty;

    [Name("Additional_Information")]
    public string AdditionalInformation { get; set; } = string.Empty;

    [Name("Notes")]
    public string Notes { get; set; } = string.Empty;

    [Name("KB_Content")]
    public string KbContent { get; set; } = string.Empty;
}
