using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Microsoft.Data.Sqlite;

namespace AmetekWatch.Storage.Tests;

/// <summary>
/// Behavioural tests for <see cref="SqliteFindingStore"/> against a real, temp-file SQLite
/// database. Each test gets its own database file (cleaned up via <see cref="Dispose"/>) so the
/// cases are independent. Expected values are hand-computed in each test.
/// </summary>
public sealed class SqliteFindingStoreTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"ametek-store-test-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        // The store pools connections; clear the pool so the file handle is released before delete.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static TriagedFinding MakeFinding(
        string url,
        string title = "Title",
        string snippet = "Snippet",
        DateTimeOffset? publishedAt = null,
        FindingCategory category = FindingCategory.OpinionSocial,
        DateTimeOffset discoveredAt = default,
        bool important = true,
        bool relevant = true,
        bool worthReporting = true,
        string rationale = "Rationale")
    {
        var finding = new Finding(url, title, snippet, publishedAt, category, discoveredAt);
        var verdict = new TriageVerdict(important, relevant, worthReporting, rationale);
        return new TriagedFinding(finding, verdict);
    }

    [Fact]
    public async Task SaveThenGetAll_RoundTripsAllFields()
    {
        var store = new SqliteFindingStore(_dbPath);
        var published = new DateTimeOffset(2026, 6, 1, 9, 30, 0, TimeSpan.FromHours(-4));
        var discovered = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        var original = MakeFinding(
            url: "https://example.com/ametek-earnings",
            title: "AMETEK beats estimates",
            snippet: "Q2 revenue up",
            publishedAt: published,
            category: FindingCategory.FinancialReport,
            discoveredAt: discovered,
            important: true,
            relevant: true,
            worthReporting: false,
            rationale: "Solid but already widely covered");

        await store.SaveAsync(original, CancellationToken.None);
        var all = await store.GetAllAsync(CancellationToken.None);

        // Hand-computed: exactly one row, every field equal to what we saved.
        Assert.Single(all);
        var got = all[0];
        Assert.Equal("https://example.com/ametek-earnings", got.Finding.Url);
        Assert.Equal("AMETEK beats estimates", got.Finding.Title);
        Assert.Equal("Q2 revenue up", got.Finding.Snippet);
        Assert.Equal(published, got.Finding.PublishedAt);
        Assert.Equal(FindingCategory.FinancialReport, got.Finding.Category);
        Assert.Equal(discovered, got.Finding.DiscoveredAt);
        Assert.True(got.Verdict.Important);
        Assert.True(got.Verdict.Relevant);
        Assert.False(got.Verdict.WorthReporting);
        Assert.Equal("Solid but already widely covered", got.Verdict.Rationale);
    }

    [Fact]
    public async Task SaveSameUrlTwice_UpsertsToOneRow_LatestWins()
    {
        var store = new SqliteFindingStore(_dbPath);
        const string url = "https://example.com/same";
        var first = MakeFinding(
            url: url,
            title: "First title",
            discoveredAt: new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
            worthReporting: false,
            rationale: "first");
        var second = MakeFinding(
            url: url,
            title: "Second title",
            discoveredAt: new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero),
            worthReporting: true,
            rationale: "second");

        await store.SaveAsync(first, CancellationToken.None);
        await store.SaveAsync(second, CancellationToken.None);
        var all = await store.GetAllAsync(CancellationToken.None);

        // Hand-computed: same Url collapses to one row; the second save's values win.
        Assert.Single(all);
        Assert.Equal("Second title", all[0].Finding.Title);
        Assert.True(all[0].Verdict.WorthReporting);
        Assert.Equal("second", all[0].Verdict.Rationale);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 11, 0, 0, 0, TimeSpan.Zero),
            all[0].Finding.DiscoveredAt);
    }

    [Fact]
    public async Task GetAll_OrdersByDiscoveredAtDescending()
    {
        var store = new SqliteFindingStore(_dbPath);
        var oldest = MakeFinding(
            url: "https://example.com/oldest",
            discoveredAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var middle = MakeFinding(
            url: "https://example.com/middle",
            discoveredAt: new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero));
        var newest = MakeFinding(
            url: "https://example.com/newest",
            discoveredAt: new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero));

        // Insert out of order to prove the ORDER BY, not insertion order, drives the result.
        await store.SaveAsync(middle, CancellationToken.None);
        await store.SaveAsync(oldest, CancellationToken.None);
        await store.SaveAsync(newest, CancellationToken.None);
        var all = await store.GetAllAsync(CancellationToken.None);

        // Hand-computed: most-recent discovered_at first → newest, middle, oldest.
        Assert.Equal(3, all.Count);
        Assert.Equal("https://example.com/newest", all[0].Finding.Url);
        Assert.Equal("https://example.com/middle", all[1].Finding.Url);
        Assert.Equal("https://example.com/oldest", all[2].Finding.Url);
    }

    [Fact]
    public async Task NullablePublishedAt_RoundTripsAsNull()
    {
        var store = new SqliteFindingStore(_dbPath);
        var finding = MakeFinding(
            url: "https://example.com/no-date",
            publishedAt: null,
            discoveredAt: new DateTimeOffset(2026, 6, 18, 8, 0, 0, TimeSpan.Zero));

        await store.SaveAsync(finding, CancellationToken.None);
        var all = await store.GetAllAsync(CancellationToken.None);

        // Hand-computed: a null PublishedAt persists as SQL NULL and comes back null.
        Assert.Single(all);
        Assert.Null(all[0].Finding.PublishedAt);
    }
}
