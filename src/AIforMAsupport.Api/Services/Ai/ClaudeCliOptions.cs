namespace AIforMAsupport.Api.Services.Ai;

public sealed class ClaudeCliOptions
{
    public const string SectionName = "ClaudeCli";

    public string ExecutablePath { get; set; } = "claude";

    // Path is relative to the API project's working directory (repo root's data/ folder, two levels up).
    public string SystemPromptFile { get; set; } = "../../data/Project_Instruction_MA_Bridgestone_v2.md";

    // เคสซ้ำ searches up to 20 cases and typically takes longest; under load (e.g. this dev
    // machine also running the Claude Desktop app) a single call has been observed to exceed
    // 90s, so this is set generously rather than tuned to the common case.
    public int TimeoutSeconds { get; set; } = 150;

    // low, medium, high, xhigh, max. This is support Q&A over KB text already narrowed down to
    // the top few matching cases server-side, not open-ended reasoning/coding - "medium" trades
    // a bit of depth for materially faster responses. Bump back up if answer quality suffers.
    public string EffortLevel { get; set; } = "medium";
}
