using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;
using Microsoft.Data.Sqlite;

namespace AmetekWatch.Storage;

/// <summary>
/// Durable <see cref="IFindingStore"/> backed by a local SQLite database. The schema is created
/// on first use if absent. Saves upsert by <see cref="Finding.Url"/> (the dedupe identity), and
/// <see cref="GetAllAsync"/> returns findings ordered by <see cref="Finding.DiscoveredAt"/>
/// descending (most-recently discovered first).
/// </summary>
/// <remarks>
/// <para>Dates are stored as ISO-8601 round-trip text (<c>DateTimeOffset</c> "O" format); the
/// <see cref="FindingCategory"/> is stored as its enum name. A null
/// <see cref="Finding.PublishedAt"/> round-trips as SQL NULL.</para>
/// </remarks>
public sealed class SqliteFindingStore : IFindingStore
{
    private readonly string _connectionString;

    /// <summary>
    /// Creates a store over the SQLite database at <paramref name="dbPath"/>, creating the schema
    /// if it does not yet exist. The file itself is created on first connection if absent.
    /// </summary>
    /// <param name="dbPath">Filesystem path to the SQLite database file.</param>
    public SqliteFindingStore(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
        }.ToString();

        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS findings (
                url             TEXT PRIMARY KEY,
                title           TEXT NOT NULL,
                snippet         TEXT NOT NULL,
                published_at    TEXT NULL,
                category        TEXT NOT NULL,
                discovered_at   TEXT NOT NULL,
                important       INTEGER NOT NULL,
                relevant        INTEGER NOT NULL,
                worth_reporting INTEGER NOT NULL,
                rationale       TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public async Task SaveAsync(TriagedFinding tf, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tf);

        var finding = tf.Finding;
        var verdict = tf.Verdict;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO findings
                (url, title, snippet, published_at, category, discovered_at,
                 important, relevant, worth_reporting, rationale)
            VALUES
                ($url, $title, $snippet, $published_at, $category, $discovered_at,
                 $important, $relevant, $worth_reporting, $rationale)
            ON CONFLICT(url) DO UPDATE SET
                title           = excluded.title,
                snippet         = excluded.snippet,
                published_at    = excluded.published_at,
                category        = excluded.category,
                discovered_at   = excluded.discovered_at,
                important       = excluded.important,
                relevant        = excluded.relevant,
                worth_reporting = excluded.worth_reporting,
                rationale       = excluded.rationale;
            """;

        command.Parameters.AddWithValue("$url", finding.Url);
        command.Parameters.AddWithValue("$title", finding.Title);
        command.Parameters.AddWithValue("$snippet", finding.Snippet);
        command.Parameters.AddWithValue(
            "$published_at",
            finding.PublishedAt is { } published ? published.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("$category", finding.Category.ToString());
        command.Parameters.AddWithValue("$discovered_at", finding.DiscoveredAt.ToString("O"));
        command.Parameters.AddWithValue("$important", verdict.Important ? 1 : 0);
        command.Parameters.AddWithValue("$relevant", verdict.Relevant ? 1 : 0);
        command.Parameters.AddWithValue("$worth_reporting", verdict.WorthReporting ? 1 : 0);
        command.Parameters.AddWithValue("$rationale", verdict.Rationale);

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TriagedFinding>> GetAllAsync(CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT url, title, snippet, published_at, category, discovered_at,
                   important, relevant, worth_reporting, rationale
            FROM findings
            ORDER BY discovered_at DESC;
            """;

        var results = new List<TriagedFinding>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var url = reader.GetString(0);
            var title = reader.GetString(1);
            var snippet = reader.GetString(2);
            DateTimeOffset? publishedAt = reader.IsDBNull(3)
                ? null
                : DateTimeOffset.Parse(
                    reader.GetString(3),
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind);
            var category = Enum.Parse<FindingCategory>(reader.GetString(4));
            var discoveredAt = DateTimeOffset.Parse(
                reader.GetString(5),
                null,
                System.Globalization.DateTimeStyles.RoundtripKind);
            var important = reader.GetInt64(6) != 0;
            var relevant = reader.GetInt64(7) != 0;
            var worthReporting = reader.GetInt64(8) != 0;
            var rationale = reader.GetString(9);

            var finding = new Finding(url, title, snippet, publishedAt, category, discoveredAt);
            var verdict = new TriageVerdict(important, relevant, worthReporting, rationale);
            results.Add(new TriagedFinding(finding, verdict));
        }

        return results;
    }
}
