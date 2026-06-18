using AmetekWatch.Core.Model;
using AmetekWatch.Core.Search;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for the pure searcher logic: <see cref="SearchQueryBuilder"/> (query construction) and
/// <see cref="SearchResultMapper"/> (result → <see cref="Finding"/> mapping + category heuristic).
/// Every expected value is hand-computed from the spec, not read back from the production code.
/// </summary>
public class SearcherLogicTests
{
    // A fixed injected discovery instant — proves ToFinding never reads a clock.
    private static readonly DateTimeOffset DiscoveredAt =
        new(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);

    // --- SearchQueryBuilder -------------------------------------------------------------------

    [Fact]
    public void BuildQueries_CoversSubjectAndBothFocusAreas_InFixedOrder()
    {
        var queries = SearchQueryBuilder.BuildQueries(new SweepQuery("AMETEK"));

        // Hand-computed expectation: general subject query, then opinion query, then financial query.
        Assert.Equal(
            new[]
            {
                "AMETEK",
                "AMETEK opinion OR commentary OR social sentiment",
                "AMETEK earnings OR financial report OR SEC filing",
            },
            queries.ToArray());

        // The subject appears verbatim as the first (general) query.
        Assert.Equal("AMETEK", queries[0]);

        // One query targets the opinion/social focus area...
        Assert.Contains(queries, q => q.Contains("opinion", StringComparison.Ordinal));
        // ...and one targets the financial-report focus area.
        Assert.Contains(queries, q => q.Contains("financial report", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildQueries_IsDeduplicatedAndDeterministic()
    {
        var first = SearchQueryBuilder.BuildQueries(new SweepQuery("AMETEK"));
        var second = SearchQueryBuilder.BuildQueries(new SweepQuery("AMETEK"));

        // No duplicate query strings.
        Assert.Equal(first.Count, first.Distinct().Count());

        // Same input → identical output (deterministic, pure).
        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void BuildQueries_TrimsSubject()
    {
        var queries = SearchQueryBuilder.BuildQueries(new SweepQuery("  AMETEK  "));

        Assert.Equal("AMETEK", queries[0]);
    }

    // --- SearchResultMapper: category heuristic -----------------------------------------------

    [Fact]
    public void ToFinding_ClassifiesSecDomain_AsFinancialReport()
    {
        var item = new SearchResultItem(
            Url: "https://www.sec.gov/cgi-bin/browse-edgar?ametek",
            Title: "AMETEK Inc filing index",
            Snippet: "Filings for AMETEK.",
            PublishedAt: null,
            SourceDomain: "sec.gov");

        Assert.Equal(FindingCategory.FinancialReport, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    [Fact]
    public void ToFinding_ClassifiesInvestorRelationsDomain_AsFinancialReport()
    {
        var item = new SearchResultItem(
            Url: "https://ir.ametek.com/news/q2",
            Title: "AMETEK Investor Relations",
            Snippet: "Latest investor news.",
            PublishedAt: null,
            SourceDomain: "ir.ametek.com");

        Assert.Equal(FindingCategory.FinancialReport, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    [Fact]
    public void ToFinding_ClassifiesEarningsTitle_AsFinancialReport()
    {
        // Neutral domain, but the title names a financial report → FinancialReport.
        var item = new SearchResultItem(
            Url: "https://news.example.com/ametek-results",
            Title: "AMETEK reports Q2 earnings beat",
            Snippet: "Quarterly results topped estimates.",
            PublishedAt: null,
            SourceDomain: "news.example.com");

        Assert.Equal(FindingCategory.FinancialReport, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    [Fact]
    public void ToFinding_ClassifiesOpinionTitle_AsOpinionSocial()
    {
        var item = new SearchResultItem(
            Url: "https://pundit.example.com/ametek-take",
            Title: "Opinion: why AMETEK is undervalued",
            Snippet: "A columnist's take.",
            PublishedAt: null,
            SourceDomain: "pundit.example.com");

        Assert.Equal(FindingCategory.OpinionSocial, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    [Fact]
    public void ToFinding_ClassifiesSocialDomain_AsOpinionSocial()
    {
        // Neutral title, but a known social domain → OpinionSocial.
        var item = new SearchResultItem(
            Url: "https://www.reddit.com/r/stocks/comments/ametek",
            Title: "AMETEK thread",
            Snippet: "Discussion about AME.",
            PublishedAt: null,
            SourceDomain: "reddit.com");

        Assert.Equal(FindingCategory.OpinionSocial, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    [Fact]
    public void ToFinding_ClassifiesNeutralItem_AsOther()
    {
        // No financial or opinion/social signal in domain or title → Other.
        var item = new SearchResultItem(
            Url: "https://news.example.com/ametek-sponsors-5k",
            Title: "AMETEK sponsors local charity 5K",
            Snippet: "Community sponsorship announcement.",
            PublishedAt: null,
            SourceDomain: "news.example.com");

        Assert.Equal(FindingCategory.Other, SearchResultMapper.ToFinding(item, DiscoveredAt).Category);
    }

    // --- SearchResultMapper: field mapping ----------------------------------------------------

    [Fact]
    public void ToFinding_MapsAllFields_AndUsesInjectedDiscoveredAt()
    {
        var publishedAt = new DateTimeOffset(2026, 6, 17, 8, 0, 0, TimeSpan.Zero);
        var item = new SearchResultItem(
            Url: "https://ir.ametek.com/q2",
            Title: "AMETEK Q2 earnings",
            Snippet: "Results summary.",
            PublishedAt: publishedAt,
            SourceDomain: "ir.ametek.com");

        var finding = SearchResultMapper.ToFinding(item, DiscoveredAt);

        Assert.Equal(item.Url, finding.Url);
        Assert.Equal(item.Title, finding.Title);
        Assert.Equal(item.Snippet, finding.Snippet);
        Assert.Equal(publishedAt, finding.PublishedAt);
        Assert.Equal(FindingCategory.FinancialReport, finding.Category);   // ir. + earnings
        Assert.Equal(DiscoveredAt, finding.DiscoveredAt);                  // injected, not a clock
    }

    [Fact]
    public void ToFinding_NullItem_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SearchResultMapper.ToFinding(null!, DiscoveredAt));
    }
}
