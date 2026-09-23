using System.Globalization;
using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.History;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.KnowledgeBase;

// Shares the same SQLite file as ConversationHistory/SavedCases (HistoryOptions:DatabasePath) -
// one small local database for all three, nothing new to configure. See IKbAdditionStore.
public sealed class SqliteKbAdditionStore : IKbAdditionStore
{
    private readonly string _connectionString;

    public SqliteKbAdditionStore(IOptions<HistoryOptions> options)
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
            CREATE TABLE IF NOT EXISTS KbAdditions (
                Id TEXT PRIMARY KEY,
                BsModule TEXT NOT NULL,
                SubCategory TEXT NOT NULL,
                CaseType TEXT NOT NULL,
                Summary TEXT NOT NULL,
                KbContent TEXT NOT NULL,
                SourceSavedCaseId INTEGER NULL,
                AddedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_KbAdditions_AddedAtUtc ON KbAdditions(AddedAtUtc);
            """;
        command.ExecuteNonQuery();
    }

    // Synchronous by design - see IKbAdditionStore.GetAll(). A local SQLite file read of a table
    // this small (expected to stay in the tens/hundreds of rows) costs single-digit milliseconds,
    // the same order of magnitude as the in-memory CSV scan it's concatenated with.
    public IReadOnlyList<KbCase> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, BsModule, SubCategory, CaseType, Summary, KbContent FROM KbAdditions";

        var results = new List<KbCase>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new KbCase(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                // Description/StepsToReproduce/AdditionalInformation/Notes: team-added cases don't
                // fill these out separately (unlike a real Mantis case) - KbContent alone is what
                // the search/prompt pipeline actually reads, so the rest are just empty.
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                reader.GetString(5)));
        }

        return results;
    }

    public async Task<IReadOnlyList<KbAdditionRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BsModule, SubCategory, CaseType, Summary, KbContent, SourceSavedCaseId, AddedAtUtc
            FROM KbAdditions
            ORDER BY AddedAtUtc DESC
            """;

        var results = new List<KbAdditionRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadRecord(reader));
        }

        return results;
    }

    public async Task<KbAdditionRecord> AddAsync(
        string bsModule,
        string subCategory,
        string caseType,
        string summary,
        string kbContent,
        int? sourceSavedCaseId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (sourceSavedCaseId is not null)
        {
            using var existingCommand = connection.CreateCommand();
            existingCommand.CommandText = """
                SELECT Id, BsModule, SubCategory, CaseType, Summary, KbContent, SourceSavedCaseId, AddedAtUtc
                FROM KbAdditions
                WHERE SourceSavedCaseId = $sourceSavedCaseId
                LIMIT 1
                """;
            existingCommand.Parameters.AddWithValue("$sourceSavedCaseId", sourceSavedCaseId.Value);
            using var existingReader = await existingCommand.ExecuteReaderAsync(cancellationToken);
            if (await existingReader.ReadAsync(cancellationToken))
            {
                return ReadRecord(existingReader);
            }
        }

        // "AI-<n>": n = one past the highest existing suffix, not a row count, so a deleted entry
        // never gets its id reused by a later addition (see CLAUDE.md - display ids double as the
        // citation the AI puts in its answer, and reusing one would misattribute an old citation).
        using var nextNumberCommand = connection.CreateCommand();
        nextNumberCommand.CommandText = "SELECT MAX(CAST(SUBSTR(Id, 4) AS INTEGER)) FROM KbAdditions WHERE Id LIKE 'AI-%'";
        var maxNumberRaw = await nextNumberCommand.ExecuteScalarAsync(cancellationToken);
        var nextNumber = (maxNumberRaw is null or DBNull ? 0 : Convert.ToInt64(maxNumberRaw)) + 1;
        var id = $"AI-{nextNumber}";

        var addedAtUtc = DateTime.UtcNow;
        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO KbAdditions (Id, BsModule, SubCategory, CaseType, Summary, KbContent, SourceSavedCaseId, AddedAtUtc)
            VALUES ($id, $bsModule, $subCategory, $caseType, $summary, $kbContent, $sourceSavedCaseId, $addedAtUtc)
            """;
        insertCommand.Parameters.AddWithValue("$id", id);
        insertCommand.Parameters.AddWithValue("$bsModule", bsModule);
        insertCommand.Parameters.AddWithValue("$subCategory", subCategory);
        insertCommand.Parameters.AddWithValue("$caseType", caseType);
        insertCommand.Parameters.AddWithValue("$summary", summary);
        insertCommand.Parameters.AddWithValue("$kbContent", kbContent);
        insertCommand.Parameters.AddWithValue("$sourceSavedCaseId", (object?)sourceSavedCaseId ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("$addedAtUtc", addedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        return new KbAdditionRecord(id, bsModule, subCategory, caseType, summary, kbContent, sourceSavedCaseId, addedAtUtc);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM KbAdditions WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private static KbAdditionRecord ReadRecord(SqliteDataReader reader) => new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetInt32(6),
        ParseUtc(reader.GetString(7)));

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
