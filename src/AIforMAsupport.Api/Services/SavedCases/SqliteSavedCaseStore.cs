using System.Globalization;
using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.History;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.SavedCases;

// Durable "บันทึกเคส" log - only the turns a person actually reviewed and clicked "บันทึก" on
// (see the save dialog in Index.cshtml/console.js), in whatever wording they finalized there.
// Separate from IConversationHistoryStore, which records every turn regardless of whether anyone
// acted on it. Shares the same SQLite file as that history store (HistoryOptions:DatabasePath) -
// one small local database for both, nothing new to configure.
public sealed class SqliteSavedCaseStore : ISavedCaseStore
{
    private readonly string _connectionString;

    public SqliteSavedCaseStore(IOptions<HistoryOptions> options)
    {
        var path = Path.GetFullPath(options.Value.DatabasePath);
        _connectionString = $"Data Source={path}";
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SavedCases (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId TEXT NOT NULL,
                Summary TEXT NOT NULL,
                ReferencedCaseIds TEXT NOT NULL,
                KnownScripts TEXT NOT NULL,
                SavedAtUtc TEXT NOT NULL,
                MantisIssueId TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SavedCases_SavedAtUtc ON SavedCases(SavedAtUtc);
            """;
        command.ExecuteNonQuery();
    }

    public async Task<SavedCase> SaveAsync(
        Guid conversationId,
        string summary,
        IReadOnlyList<string> referencedCaseIds,
        IReadOnlyList<string> knownScripts,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var savedAtUtc = DateTime.UtcNow;
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO SavedCases (ConversationId, Summary, ReferencedCaseIds, KnownScripts, SavedAtUtc, MantisIssueId)
            VALUES ($conversationId, $summary, $referencedCaseIds, $knownScripts, $savedAtUtc, NULL);
            SELECT last_insert_rowid();
            """;
        insertCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
        insertCommand.Parameters.AddWithValue("$summary", summary);
        insertCommand.Parameters.AddWithValue("$referencedCaseIds", string.Join(',', referencedCaseIds));
        insertCommand.Parameters.AddWithValue("$knownScripts", string.Join(',', knownScripts));
        insertCommand.Parameters.AddWithValue("$savedAtUtc", savedAtUtc.ToString("O", CultureInfo.InvariantCulture));

        var newId = (long)(await insertCommand.ExecuteScalarAsync(cancellationToken))!;

        return new SavedCase((int)newId, conversationId, summary, referencedCaseIds, knownScripts, savedAtUtc, null);
    }

    public async Task<IReadOnlyList<SavedCase>> ListAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ConversationId, Summary, ReferencedCaseIds, KnownScripts, SavedAtUtc, MantisIssueId
            FROM SavedCases
            ORDER BY SavedAtUtc DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<SavedCase>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var referencedCaseIdsRaw = reader.GetString(3);
            List<string> referencedCaseIds = referencedCaseIdsRaw.Length == 0
                ? []
                : referencedCaseIdsRaw.Split(',').ToList();

            var knownScriptsRaw = reader.GetString(4);
            List<string> knownScripts = knownScriptsRaw.Length == 0
                ? []
                : knownScriptsRaw.Split(',').ToList();

            results.Add(new SavedCase(
                reader.GetInt32(0),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                referencedCaseIds,
                knownScripts,
                ParseUtc(reader.GetString(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }

        return results;
    }

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
