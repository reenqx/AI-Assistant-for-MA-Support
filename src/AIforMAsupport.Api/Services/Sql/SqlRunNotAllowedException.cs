namespace AIforMAsupport.Api.Services.Sql;

// Distinguishes a rejected request (feature disabled, unknown script, write script) from a real
// SqlException (DB reachable but the query itself failed) so SqlController can return 400 vs 502.
public sealed class SqlRunNotAllowedException(string message) : Exception(message);
