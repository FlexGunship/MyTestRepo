using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AmetekWatch.Core.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AmetekWatch.Web.Tests;

/// <summary>
/// End-to-end tests for the dashboard's JSON endpoint, driven through
/// <see cref="WebApplicationFactory{TEntryPoint}"/> against the real seeded app.
/// </summary>
/// <remarks>
/// Expected results are hand-computed from <see cref="AmetekWatch.Core.Pipeline.FakeSearcher"/>
/// + <see cref="AmetekWatch.Core.Pipeline.FakeTriageDecider"/> run through the SweepRunner:
/// <list type="number">
///   <item>FakeSearcher returns 5 canned findings; the duplicate <c>ametek-analyst-note</c>
///   (the "(reblog)" copy) is dropped by URL-dedupe, leaving <b>4</b> persisted findings.</item>
///   <item>The endpoint orders by <c>DiscoveredAt</c> descending. The anchor hours are
///   analyst-note=+9h, q2-earnings=+10h, 5k-sponsor=+11h, 10q=+8h, so the most-recent-first
///   order is: 5k-sponsor, q2-earnings, analyst-note, 10q.</item>
///   <item>FakeTriageDecider marks OpinionSocial + FinancialReport worth-reporting and Other
///   not, so the 3 worth-reporting findings are analyst-note, q2-earnings, and 10q; the
///   5k-sponsor (Other) is the only non-reportable one.</item>
/// </list>
/// </remarks>
public sealed class FindingsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WebApplicationFactory<Program> _factory;

    public FindingsApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<IReadOnlyList<TriagedFinding>> GetFindingsAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/findings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var findings = await response.Content.ReadFromJsonAsync<List<TriagedFinding>>(JsonOptions);
        Assert.NotNull(findings);
        return findings!;
    }

    [Fact]
    public async Task GetFindings_ReturnsTheFourPersistedFindings_DuplicateDropped()
    {
        var findings = await GetFindingsAsync();

        // 5 canned − 1 duplicate URL = 4 persisted.
        Assert.Equal(4, findings.Count);

        // The "(reblog)" syndicated copy must have been deduped away.
        Assert.DoesNotContain(findings, f => f.Finding.Title.Contains("reblog"));
        Assert.Single(findings, f => f.Finding.Url == "https://news.example.com/ametek-analyst-note");
    }

    [Fact]
    public async Task GetFindings_OrdersMostRecentDiscoveredFirst()
    {
        var findings = await GetFindingsAsync();

        var urlsInOrder = findings.Select(f => f.Finding.Url).ToArray();
        Assert.Equal(
            new[]
            {
                "https://local.example.com/ametek-5k-sponsor",   // +11h
                "https://ir.example.com/ametek-q2-earnings",     // +10h
                "https://news.example.com/ametek-analyst-note",  // +9h
                "https://sec.example.com/ametek-10q",            // +8h
            },
            urlsInOrder);
    }

    [Fact]
    public async Task GetFindings_WorthReportingSubsetMatchesTheTriageRule()
    {
        var findings = await GetFindingsAsync();

        var worthReportingUrls = findings
            .Where(f => f.Verdict.WorthReporting)
            .Select(f => f.Finding.Url)
            .OrderBy(u => u)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "https://ir.example.com/ametek-q2-earnings",     // FinancialReport
                "https://news.example.com/ametek-analyst-note",  // OpinionSocial
                "https://sec.example.com/ametek-10q",            // FinancialReport
            },
            worthReportingUrls);

        // The lone Other-category finding is the only non-reportable one.
        var notReportable = Assert.Single(findings, f => !f.Verdict.WorthReporting);
        Assert.Equal("https://local.example.com/ametek-5k-sponsor", notReportable.Finding.Url);
        Assert.Equal(FindingCategory.Other, notReportable.Finding.Category);
    }
}
