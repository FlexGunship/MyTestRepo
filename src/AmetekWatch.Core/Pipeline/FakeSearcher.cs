using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Deterministic stand-in for the real (Sonnet 4.6 + web search) searcher tier. Returns a
/// fixed list that deliberately exercises the orchestrator: it contains a duplicate URL and
/// one finding of each <see cref="FindingCategory"/>. No I/O, no network, no API key.
/// </summary>
/// <remarks>
/// The list and its timestamps are stable so tests can hand-compute expected dedupe, digest,
/// and ordering results. The duplicate (<c>url-a</c>) appears twice; first occurrence wins.
/// </remarks>
public sealed class FakeSearcher : ISearcher
{
    // A fixed wall-clock anchor for the canned findings (UTC). Distinct hours make the
    // DiscoveredAt ordering unambiguous for tests.
    private static readonly DateTimeOffset Anchor =
        new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The canned findings, in discovery order. Exposed for test oracles.</summary>
    public static readonly IReadOnlyList<Finding> Canned = new[]
    {
        new Finding(
            Url: "https://news.example.com/ametek-analyst-note",
            Title: "AMETEK shares climb on upbeat analyst note",
            Snippet: "Commentary roundup on AMETEK's latest guidance.",
            PublishedAt: Anchor.AddDays(-1),
            Category: FindingCategory.OpinionSocial,
            DiscoveredAt: Anchor.AddHours(9)),

        new Finding(
            Url: "https://ir.example.com/ametek-q2-earnings",
            Title: "AMETEK reports Q2 earnings beat",
            Snippet: "Quarterly results topped consensus estimates.",
            PublishedAt: Anchor.AddDays(-2),
            Category: FindingCategory.FinancialReport,
            DiscoveredAt: Anchor.AddHours(10)),

        new Finding(
            Url: "https://local.example.com/ametek-5k-sponsor",
            Title: "AMETEK sponsors local charity 5K",
            Snippet: "Community sponsorship announcement.",
            PublishedAt: null,
            Category: FindingCategory.Other,
            DiscoveredAt: Anchor.AddHours(11)),

        // Duplicate of the first finding's URL — must be dropped (first occurrence wins).
        new Finding(
            Url: "https://news.example.com/ametek-analyst-note",
            Title: "AMETEK shares climb on upbeat analyst note (reblog)",
            Snippet: "Syndicated copy of the analyst-note story.",
            PublishedAt: Anchor.AddDays(-1),
            Category: FindingCategory.OpinionSocial,
            DiscoveredAt: Anchor.AddHours(12)),

        new Finding(
            Url: "https://sec.example.com/ametek-10q",
            Title: "AMETEK files Form 10-Q",
            Snippet: "Quarterly SEC filing now available.",
            PublishedAt: Anchor.AddDays(-3),
            Category: FindingCategory.FinancialReport,
            DiscoveredAt: Anchor.AddHours(8)),
    };

    public Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<Finding> results = query.MaxResults is int cap && cap < Canned.Count
            ? Canned.Take(cap).ToList()
            : Canned.ToList();
        return Task.FromResult(results);
    }
}
