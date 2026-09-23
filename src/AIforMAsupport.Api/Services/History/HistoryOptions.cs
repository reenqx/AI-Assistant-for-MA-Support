namespace AIforMAsupport.Api.Services.History;

public sealed class HistoryOptions
{
    public const string SectionName = "History";

    // Relative to the API project's working directory. A plain local SQLite file - no server,
    // durable across restarts, and query-able for the conversation list UI (unlike a JSON-lines
    // log, which would need a full-file scan to group by conversation).
    public string DatabasePath { get; set; } = "conversation_history.db";
}
