using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.Tests;

/// <summary>
/// Per-finding triage isolation in <see cref="SweepRunner"/> (spec 034): a decider that throws on
/// one finding must skip <b>only</b> that finding — others persist + digest, the callback fires
/// once, and the sweep returns normally instead of aborting.
///
/// Hand-computed oracle (3 distinct URLs, all would-be worth-reporting):
///   A url-a  OpinionSocial    09:00  -> triaged OK -> persisted, worth-reporting
///   B url-b  FinancialReport  10:00  -> decider THROWS -> skipped (not persisted, not digested)
///   C url-c  OpinionSocial    11:00  -> triaged OK -> persisted, worth-reporting
/// => persisted = {A, C} (2); digest = {C, A} desc by DiscoveredAt (2); onTriageError called once for B.
/// </summary>
public class SweepRunnerResilienceTests
{
    private const string UrlA = "https://example.com/a";
    private const string UrlB = "https://example.com/b";
    private const string UrlC = "https://example.com/c";

    private static readonly DateTimeOffset Anchor = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    private static Finding Make(string url, FindingCategory cat, int hour) =>
        new(url, $"title {url}", "snippet", null, cat, Anchor.AddHours(hour));

    [Fact]
    public async Task DeciderThrowsOnOneFinding_ThatFindingSkipped_OthersPersisted_CallbackOnce()
    {
        var a = Make(UrlA, FindingCategory.OpinionSocial, 9);
        var b = Make(UrlB, FindingCategory.FinancialReport, 10);
        var c = Make(UrlC, FindingCategory.OpinionSocial, 11);

        var store = new InMemoryFindingStore();
        var errors = new List<(Finding finding, Exception ex)>();

        var runner = new SweepRunner(
            new StubSearcher(a, b, c),
            new ThrowOnUrlDecider(UrlB),
            store,
            onTriageError: (f, ex) => errors.Add((f, ex)));

        var digest = await runner.RunAsync(new SweepQuery("AMETEK"));
        var persisted = await store.GetAllAsync(CancellationToken.None);

        // B was skipped: not persisted, not digested.
        Assert.DoesNotContain(persisted, t => t.Finding.Url == UrlB);
        Assert.DoesNotContain(digest, t => t.Finding.Url == UrlB);

        // A and C persisted (2) and both in the digest (both worth-reporting).
        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, t => t.Finding.Url == UrlA);
        Assert.Contains(persisted, t => t.Finding.Url == UrlC);
        Assert.Equal(new[] { UrlC, UrlA }, digest.Select(t => t.Finding.Url).ToArray()); // 11:00, 09:00

        // Callback fired exactly once, for B, with the decider's exception.
        Assert.Single(errors);
        Assert.Equal(UrlB, errors[0].finding.Url);
        Assert.IsType<InvalidOperationException>(errors[0].ex);
    }

    /// <summary>Minimal searcher returning a fixed list — an independent oracle.</summary>
    private sealed class StubSearcher : ISearcher
    {
        private readonly IReadOnlyList<Finding> _findings;

        public StubSearcher(params Finding[] findings) => _findings = findings;

        public Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct)
            => Task.FromResult(_findings);
    }

    /// <summary>Decider that throws on one specific URL and approves every other finding.</summary>
    private sealed class ThrowOnUrlDecider : ITriageDecider
    {
        private readonly string _throwUrl;

        public ThrowOnUrlDecider(string throwUrl) => _throwUrl = throwUrl;

        public Task<TriageVerdict> JudgeAsync(Finding finding, CancellationToken ct)
        {
            if (finding.Url == _throwUrl)
            {
                throw new InvalidOperationException($"cannot triage {finding.Url}");
            }

            return Task.FromResult(new TriageVerdict(
                Important: true, Relevant: true, WorthReporting: true,
                Rationale: "approved by stub"));
        }
    }
}
