using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for the spec-038 "only new" digest: <see cref="SweepRunner"/> with
/// <c>digestOnlyNew = true</c> reports only worth-reporting findings whose <see cref="Finding.Url"/>
/// was NOT already in the store before the sweep. Every expected value is hand-computed from
/// <see cref="FakeSearcher.Canned"/> (an independent oracle).
///
/// Hand computation of the worth-reporting subset of <see cref="FakeSearcher.Canned"/>:
///   url-a  OpinionSocial    09:00  -> worth-reporting
///   url-b  FinancialReport  10:00  -> worth-reporting
///   url-c  Other            11:00  -> NOT worth-reporting
///   url-d  FinancialReport  08:00  -> worth-reporting
/// => worth-reporting (desc by DiscoveredAt) = url-b(10:00), url-a(09:00), url-d(08:00).
///
/// We pre-seed the store with url-b before the sweep. So:
///   digestOnlyNew=true  => report worth-reporting AND new = url-a, url-d (url-b excluded) = [url-a, url-d].
///   digestOnlyNew=false => report all worth-reporting (unchanged)               = [url-b, url-a, url-d].
/// In both cases all 4 unique findings (incl. url-b) are still persisted.
/// </summary>
public class SweepRunnerOnlyNewTests
{
    private const string UrlA = "https://news.example.com/ametek-analyst-note";
    private const string UrlB = "https://ir.example.com/ametek-q2-earnings";
    private const string UrlC = "https://local.example.com/ametek-5k-sponsor";
    private const string UrlD = "https://sec.example.com/ametek-10q";

    /// <summary>Pre-seeds the store with a worth-reporting finding at the given URL.</summary>
    private static async Task<InMemoryFindingStore> SeededStoreAsync(string url)
    {
        var store = new InMemoryFindingStore();
        var seed = new TriagedFinding(
            new Finding(url, "seeded prior run", "s", null,
                FindingCategory.FinancialReport, new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero)),
            new TriageVerdict(Important: true, Relevant: true, WorthReporting: true, Rationale: "seed"));
        await store.SaveAsync(seed, CancellationToken.None);
        return store;
    }

    [Fact]
    public async Task OnlyNew_ExcludesKnownUrl_IncludesNew_ButStillPersistsKnown()
    {
        var store = await SeededStoreAsync(UrlB); // url-b already known before the sweep
        var runner = new SweepRunner(
            new FakeSearcher(), new FakeTriageDecider(), store, digestOnlyNew: true);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        // url-b (known) excluded; new worth-reporting url-a, url-d included, desc by DiscoveredAt.
        Assert.Equal(new[] { UrlA, UrlD }, digest.Select(t => t.Finding.Url).ToArray());
        Assert.DoesNotContain(digest, t => t.Finding.Url == UrlB);

        // url-b is still persisted (persist-all is unchanged); url-c (Other) too, but never digested.
        Assert.Contains(persisted, t => t.Finding.Url == UrlB);
        Assert.Contains(persisted, t => t.Finding.Url == UrlC);
        Assert.DoesNotContain(digest, t => t.Finding.Url == UrlC);
    }

    [Fact]
    public async Task OnlyNewFalse_WithSameSeed_ReportsAllWorthReporting()
    {
        var store = await SeededStoreAsync(UrlB);
        var runner = new SweepRunner(
            new FakeSearcher(), new FakeTriageDecider(), store, digestOnlyNew: false);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));

        // Unchanged behaviour: all worth-reporting in the digest, incl. the known url-b.
        Assert.Equal(new[] { UrlB, UrlA, UrlD }, digest.Select(t => t.Finding.Url).ToArray());
    }

    [Fact]
    public async Task OnlyNew_EmptyStore_AllWorthReportingAreNew()
    {
        // No pre-seed: every worth-reporting finding is new, so onlyNew matches the default digest.
        var store = new InMemoryFindingStore();
        var runner = new SweepRunner(
            new FakeSearcher(), new FakeTriageDecider(), store, digestOnlyNew: true);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));

        Assert.Equal(new[] { UrlB, UrlA, UrlD }, digest.Select(t => t.Finding.Url).ToArray());
    }
}
