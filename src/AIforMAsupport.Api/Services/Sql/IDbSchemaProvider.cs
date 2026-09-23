namespace AIforMAsupport.Api.Services.Sql;

// What goes into the prompt as database structure, and which tables that covers (their full
// "schema.Table" names, so the caller can remember them for the conversation's next turn).
public sealed record SchemaSelection(string Text, IReadOnlyList<string> Tables);

public interface IDbSchemaProvider
{
    // The always-included reference notes plus the full definition (columns, PK, FK) of the tables
    // that look relevant to this request. priorityText is text that names tables the answer will
    // almost certainly use (the matched Known Scripts' SQL) and counts for more; contextText is
    // everything else that may mention tables (the question, matched cases, the previous answer).
    //
    // caseTexts are texts whose own SQL says where the work happens, one entry each - the retrieved
    // cases and, on a follow-up, the previous answer: the tables a text's SQL touches
    // (FROM / JOIN / UPDATE / ...) are counted per text and weigh as much as tables in priorityText.
    //
    // carriedTables are the tables used on the conversation's previous AI turn; they get a small
    // bonus so a follow-up keeps working with the same tables unless the new evidence is stronger.
    //
    // Text is empty if no schema files are available - callers should treat that as "no schema
    // reference available" and skip appending it, not as an error.
    SchemaSelection GetSchema(
        string priorityText,
        string contextText,
        IReadOnlyList<string>? caseTexts = null,
        IReadOnlyCollection<string>? carriedTables = null);
}
