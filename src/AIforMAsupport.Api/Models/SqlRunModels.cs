namespace AIforMAsupport.Api.Models;

// Exactly one of ScriptName/SqlText should be set - see ISqlScriptRunner.
public sealed record SqlRunRequest(string? ScriptName, string? SqlText, IReadOnlyDictionary<string, string> Parameters);

public sealed record SqlResultTable(string? Label, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows);

public sealed record SqlRunResponse(IReadOnlyList<SqlResultTable> Tables);
