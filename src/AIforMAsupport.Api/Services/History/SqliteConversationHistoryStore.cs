using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.History;

public sealed class SqliteConversationHistoryStore : IConversationHistoryStore
{
    private readonly string _connectionString;

    public SqliteConversationHistoryStore(IOptions<HistoryOptions> options)
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
            CREATE TABLE IF NOT EXISTS ConversationTurns (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId TEXT NOT NULL,
                TurnIndex INTEGER NOT NULL,
                Question TEXT NOT NULL,
                Answer TEXT NOT NULL,
                ReferencedCaseIds TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_ConversationTurns_ConversationId ON ConversationTurns(ConversationId);
            CREATE INDEX IF NOT EXISTS IX_ConversationTurns_CreatedAtUtc ON ConversationTurns(CreatedAtUtc);
            """;
        command.ExecuteNonQuery();
    }

    public async Task AppendAsync(
        Guid conversationId,
        string question,
        string answer,
        IReadOnlyList<string> referencedCaseIds,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var nextIndexCommand = connection.CreateCommand();
        nextIndexCommand.CommandText = "SELECT COALESCE(MAX(TurnIndex), 0) + 1 FROM ConversationTurns WHERE ConversationId = $conversationId";
        nextIndexCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
        var turnIndex = (long)(await nextIndexCommand.ExecuteScalarAsync(cancellationToken))!;

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO ConversationTurns (ConversationId, TurnIndex, Question, Answer, ReferencedCaseIds, CreatedAtUtc)
            VALUES ($conversationId, $turnIndex, $question, $answer, $referencedCaseIds, $createdAtUtc)
            """;
        insertCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
        insertCommand.Parameters.AddWithValue("$turnIndex", turnIndex);
        insertCommand.Parameters.AddWithValue("$question", question);
        insertCommand.Parameters.AddWithValue("$answer", answer);
        insertCommand.Parameters.AddWithValue("$referencedCaseIds", string.Join(',', referencedCaseIds));
        insertCommand.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                t1.ConversationId,
                (SELECT Question FROM ConversationTurns t2
                 WHERE t2.ConversationId = t1.ConversationId
                 ORDER BY t2.TurnIndex ASC LIMIT 1) AS FirstQuestion,
                MAX(t1.CreatedAtUtc) AS LastActivityUtc,
                COUNT(*) AS TurnCount
            FROM ConversationTurns t1
            GROUP BY t1.ConversationId
            ORDER BY LastActivityUtc DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<ConversationSummary>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ConversationSummary(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                ParseUtc(reader.GetString(2)),
                reader.GetInt32(3)));
        }

        return results;
    }

    public async Task<IReadOnlyList<HistoryTurn>> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TurnIndex, Question, Answer, ReferencedCaseIds, CreatedAtUtc
            FROM ConversationTurns
            WHERE ConversationId = $conversationId
            ORDER BY TurnIndex ASC
            """;
        command.Parameters.AddWithValue("$conversationId", conversationId.ToString());

        var results = new List<HistoryTurn>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var referencedCaseIdsRaw = reader.GetString(3);
            List<string> referencedCaseIds = referencedCaseIdsRaw.Length == 0
                ? []
                : referencedCaseIdsRaw.Split(',').ToList();

            results.Add(new HistoryTurn(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                referencedCaseIds,
                ParseUtc(reader.GetString(4))));
        }

        return results;
    }

    private static DateTime ParseUtc(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
