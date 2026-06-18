using AmetekWatch.Anthropic;
using AmetekWatch.Core.Model;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// End-to-end (offline) oracles for <see cref="AnthropicSearcher"/>: driven by a
/// <see cref="FakeMessagesClient"/> returning a canned JSON array, it yields the expected
/// <c>Finding</c>s — with categories assigned by the REAL 013 <c>SearchResultMapper</c> and discovery
/// stamped from the injected clock, never a wall clock. No network.
/// </summary>
public class AnthropicSearcherTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    // Three hits chosen to exercise each real-mapper category boundary:
    //   sec.gov domain      -> FinancialReport
    //   "opinion" in title  -> OpinionSocial
    //   neutral             -> Other
    private const string ThreeHits = """
        [
          {
            "url": "https://sec.gov/edgar/ame-10q",
            "title": "AMETEK quarterly filing",
            "snippet": "Q2 results.",
            "publishedAt": "2026-06-10T09:00:00+00:00",
            "sourceDomain": "sec.gov"
          },
          {
            "url": "https://news.example.com/ame-opinion",
            "title": "Opinion: AMETEK's acquisition spree",
            "snippet": "A personal take.",
            "publishedAt": null,
            "sourceDomain": "news.example.com"
          },
          {
            "url": "https://news.example.com/ame-update",
            "title": "AMETEK names new plant manager",
            "snippet": "Routine corporate update.",
            "publishedAt": null,
            "sourceDomain": "news.example.com"
          }
        ]
        """;

    private static AnthropicSearcher SearcherReturning(string json) => new(
        new FakeMessagesClient(json),
        new SearchRequestFactory(),
        new SearchResponseParser(),
        () => FixedNow);

    [Fact]
    public async Task SweepAsync_MapsHitsToFindingsWithRealMapperCategories()
    {
        var findings = await SearcherReturning(ThreeHits)
            .SweepAsync(new SweepQuery("AMETEK"), CancellationToken.None);

        Assert.Equal(3, findings.Count);

        Assert.Equal("https://sec.gov/edgar/ame-10q", findings[0].Url);
        Assert.Equal(FindingCategory.FinancialReport, findings[0].Category);

        Assert.Equal(FindingCategory.OpinionSocial, findings[1].Category);

        Assert.Equal(FindingCategory.Other, findings[2].Category);
    }

    [Fact]
    public async Task SweepAsync_StampsDiscoveredAtFromInjectedClock()
    {
        var findings = await SearcherReturning(ThreeHits)
            .SweepAsync(new SweepQuery("AMETEK"), CancellationToken.None);

        Assert.All(findings, f => Assert.Equal(FixedNow, f.DiscoveredAt));
    }

    [Fact]
    public async Task SweepAsync_EmptyArray_YieldsNoFindings()
    {
        var findings = await SearcherReturning("[]")
            .SweepAsync(new SweepQuery("AMETEK"), CancellationToken.None);

        Assert.Empty(findings);
    }

    [Fact]
    public async Task SweepAsync_SendsSonnet46Request()
    {
        var fake = new FakeMessagesClient(ThreeHits);
        var searcher = new AnthropicSearcher(
            fake, new SearchRequestFactory(), new SearchResponseParser(), () => FixedNow);

        await searcher.SweepAsync(new SweepQuery("AMETEK"), CancellationToken.None);

        Assert.NotNull(fake.LastRequest);
        Assert.Contains("claude-sonnet-4-6", fake.LastRequest!.Model.ToString(), StringComparison.Ordinal);
        Assert.True(Assert.Single(fake.LastRequest.Tools!).TryPickWebSearchTool20260209(out _));
    }

    [Fact]
    public void Ctor_NullArgs_Throw()
    {
        var factory = new SearchRequestFactory();
        var parser = new SearchResponseParser();
        var client = new FakeMessagesClient("[]");
        Func<DateTimeOffset> clock = () => FixedNow;

        Assert.Throws<ArgumentNullException>(() => new AnthropicSearcher(null!, factory, parser, clock));
        Assert.Throws<ArgumentNullException>(() => new AnthropicSearcher(client, null!, parser, clock));
        Assert.Throws<ArgumentNullException>(() => new AnthropicSearcher(client, factory, null!, clock));
        Assert.Throws<ArgumentNullException>(() => new AnthropicSearcher(client, factory, parser, null!));
    }
}
