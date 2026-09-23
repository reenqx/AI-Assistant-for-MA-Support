using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.Sql;

public interface ISqlScriptRunner
{
    // Exactly one of scriptName/sqlText should be set: scriptName must match a KnownScript.Name
    // exactly (the pre-vetted path); sqlText is raw SQL text supplied directly (e.g. SQL the AI
    // wrote itself) - the user explicitly chose to allow this as long as it's read-only, see
    // CLAUDE.md. Either way this never accepts a write statement.
    Task<SqlRunResponse> RunAsync(
        string? scriptName,
        string? sqlText,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken);
}
