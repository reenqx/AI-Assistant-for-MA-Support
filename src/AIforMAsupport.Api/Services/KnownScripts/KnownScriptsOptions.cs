namespace AIforMAsupport.Api.Services.KnownScripts;

public sealed class KnownScriptsOptions
{
    public const string SectionName = "KnownScripts";

    // Path is relative to the API project's working directory (repo root's data/ folder, two levels up).
    public string Directory { get; set; } = "../../data/KnownScripts";
}
