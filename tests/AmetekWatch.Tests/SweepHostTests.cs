using AmetekWatch.App;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Microsoft.Data.Sqlite;

namespace AmetekWatch.Tests;

/// <summary>
/// Behavioural tests for <see cref="SweepHost"/> over a real <b>temp-file SQLite</b> store and the
/// deterministic Core fakes. Every expected value is hand-computed from
/// <see cref="FakeSearcher.Canned"/> (an independent oracle), not read back from the code.
///
/// Hand computation of <see cref="FakeSearcher.Canned"/> (5 entries, in discovery order):
///   1. url-a  OpinionSocial    09:00  -> kept,  worth-reporting
///   2. url-b  FinancialReport  10:00  -> kept,  worth-reporting
///   3. url-c  Other            11:00  -> kept,  NOT worth-reporting
///   4. url-a  OpinionSocial    12:00  -> DROPPED (duplicate of #1; first occurrence wins)
///   5. url-d  FinancialReport  08:00  -> kept,  worth-reporting
/// => 4 unique persisted; 3 worth-reporting; digest order by DiscoveredAt desc = url-b, url-a, url-d.
/// </summary>
public sealed class SweepHostTests : IDisposable
{
    private const string UrlA = "https://news.example.com/ametek-analyst-note";
    private const string UrlB = "https://ir.example.com/ametek-q2-earnings";
    private const string UrlC = "https://local.example.com/ametek-5k-sponsor";
    private const string UrlD = "https://sec.example.com/ametek-10q";

    // A unique temp-file DB per test instance — index makes the discovery order unambiguous.
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"ametek-watch-test-{Guid.NewGuid():N}.db");

    private SweepHost BuildHost(out SqliteFindingStore store)
    {
        store = new SqliteFindingStore(_dbPath);
        var options = new SweepOptions { Subject = "AMETEK", IntervalMinutes = 1440, RunOnce = true };
        return new SweepHost(new FakeSearcher(), new FakeTriageDecider(), store, options);
    }

    [Fact]
    public async Task RunOnceAsync_PersistsFourUnique_ReturnsThreeWorthReporting()
    {
        var host = BuildHost(out var store);

        var digest = await host.RunOnceAsync(CancellationToken.None);
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.Equal(4, persisted.Count);   // 5 found - 1 duplicate URL
        Assert.Equal(3, digest.Count);      // a, b, d worth-reporting; c (Other) is not
        Assert.All(digest, t => Assert.True(t.Verdict.WorthReporting));
    }

    [Fact]
    public async Task RunOnceAsync_DigestOrderedMostRecentDiscoveredAtFirst()
    {
        var host = BuildHost(out _);

        var digest = await host.RunOnceAsync(CancellationToken.None);

        // Hand-computed: b(10:00), a(09:00), d(08:00).
        Assert.Equal(new[] { UrlB, UrlA, UrlD }, digest.Select(t => t.Finding.Url).ToArray());
    }

    [Fact]
    public async Task RunOnceAsync_WritesDbFile_NonWorthIsPersistedButNotDigested()
    {
        var host = BuildHost(out var store);

        var digest = await host.RunOnceAsync(CancellationToken.None);
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.True(File.Exists(_dbPath));                                  // durable file written
        Assert.DoesNotContain(digest, t => t.Finding.Url == UrlC);         // Other not in digest
        Assert.Contains(persisted, t => t.Finding.Url == UrlC);            // ...but still persisted
        Assert.False(persisted.Single(t => t.Finding.Url == UrlC).Verdict.WorthReporting);
    }

    [Fact]
    public async Task RunOnceAsync_PersistedSurvivesAFreshStoreOverSameFile()
    {
        var host = BuildHost(out _);
        await host.RunOnceAsync(CancellationToken.None);

        // Re-open the same file with a brand-new store: the 4 unique findings must read back.
        var reopened = new SqliteFindingStore(_dbPath);
        var persisted = await reopened.GetAllAsync(CancellationToken.None);

        Assert.Equal(4, persisted.Count);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections; clear them so the temp file can be deleted.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
