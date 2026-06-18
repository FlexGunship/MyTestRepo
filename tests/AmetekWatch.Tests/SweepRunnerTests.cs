using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.Tests;

/// <summary>
/// Behavioural tests for <see cref="SweepRunner"/>. Every expected value is hand-computed from
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
public class SweepRunnerTests
{
    private const string UrlA = "https://news.example.com/ametek-analyst-note";
    private const string UrlB = "https://ir.example.com/ametek-q2-earnings";
    private const string UrlC = "https://local.example.com/ametek-5k-sponsor";
    private const string UrlD = "https://sec.example.com/ametek-10q";

    private static SweepRunner BuildRunner(ISearcher searcher, out InMemoryFindingStore store)
    {
        store = new InMemoryFindingStore();
        return new SweepRunner(searcher, new FakeTriageDecider(), store);
    }

    [Fact]
    public async Task HappyPath_PersistsFourUnique_DigestHasThreeWorthReporting()
    {
        var runner = BuildRunner(new FakeSearcher(), out var store);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.Equal(4, persisted.Count);   // 5 found - 1 duplicate
        Assert.Equal(3, digest.Count);      // a, b, d worth-reporting; c is not
    }

    [Fact]
    public async Task Dedupe_DuplicateUrlPersistedExactlyOnce_FirstOccurrenceWins()
    {
        // Independent two-item stub: same URL twice, different DiscoveredAt. First must win.
        var first = new Finding(UrlA, "first", "s", null,
            FindingCategory.OpinionSocial, new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero));
        var second = first with
        {
            Title = "second",
            DiscoveredAt = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
        };
        var runner = BuildRunner(new StubSearcher(first, second), out var store);

        await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.Single(persisted);
        Assert.Equal(UrlA, persisted[0].Finding.Url);
        Assert.Equal("first", persisted[0].Finding.Title); // first occurrence kept, not "second"
    }

    [Fact]
    public async Task DedupeOverFake_UrlAPersistedExactlyOnce()
    {
        var runner = BuildRunner(new FakeSearcher(), out var store);

        await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.Equal(1, persisted.Count(t => t.Finding.Url == UrlA));
    }

    [Fact]
    public async Task DigestFilter_OnlyWorthReporting_ButNonWorthIsStillPersisted()
    {
        var runner = BuildRunner(new FakeSearcher(), out var store);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        // Every digest entry is worth-reporting.
        Assert.All(digest, t => Assert.True(t.Verdict.WorthReporting));
        // url-c (Other) is NOT in the digest...
        Assert.DoesNotContain(digest, t => t.Finding.Url == UrlC);
        // ...but IS persisted.
        Assert.Contains(persisted, t => t.Finding.Url == UrlC);
        Assert.False(persisted.Single(t => t.Finding.Url == UrlC).Verdict.WorthReporting);
    }

    [Fact]
    public async Task Ordering_DigestIsMostRecentDiscoveredAtFirst()
    {
        var runner = BuildRunner(new FakeSearcher(), out _);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));

        // Hand-computed: b(10:00), a(09:00), d(08:00).
        Assert.Equal(new[] { UrlB, UrlA, UrlD }, digest.Select(t => t.Finding.Url).ToArray());

        // And it is genuinely sorted descending.
        for (var i = 1; i < digest.Count; i++)
        {
            Assert.True(digest[i - 1].Finding.DiscoveredAt >= digest[i].Finding.DiscoveredAt);
        }
    }

    [Fact]
    public async Task TriageRule_OtherIsNotWorthReporting_FinancialAndOpinionAre()
    {
        var runner = BuildRunner(new FakeSearcher(), out var store);

        await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.True(persisted.Single(t => t.Finding.Url == UrlA).Verdict.WorthReporting);  // OpinionSocial
        Assert.True(persisted.Single(t => t.Finding.Url == UrlB).Verdict.WorthReporting);  // FinancialReport
        Assert.True(persisted.Single(t => t.Finding.Url == UrlD).Verdict.WorthReporting);  // FinancialReport
        Assert.False(persisted.Single(t => t.Finding.Url == UrlC).Verdict.WorthReporting); // Other
    }

    [Fact]
    public async Task MaxResults_CapsTheSweepInput()
    {
        // Cap to the first 2 canned findings (url-a, url-b): both kept, both worth-reporting.
        var runner = BuildRunner(new FakeSearcher(), out var store);

        var digest = await runner.RunAsync(new SweepQuery("AMETEK", MaxResults: 2));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, persisted.Count);
        Assert.Equal(2, digest.Count);
        Assert.Equal(new[] { UrlB, UrlA }, digest.Select(t => t.Finding.Url).ToArray()); // 10:00 then 09:00
    }

    /// <summary>Minimal searcher returning a fixed list — an independent oracle for dedupe.</summary>
    private sealed class StubSearcher : ISearcher
    {
        private readonly IReadOnlyList<Finding> _findings;

        public StubSearcher(params Finding[] findings) => _findings = findings;

        public Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct)
            => Task.FromResult(_findings);
    }
}
