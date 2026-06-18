using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AmetekWatch.Core.Model;
using AmetekWatch.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AmetekWatch.Web.Tests;

/// <summary>
/// End-to-end tests for the dashboard's JSON endpoint, driven through
/// <see cref="WebApplicationFactory{TEntryPoint}"/> against a <b>temp SQLite database</b> that is
/// the dashboard's real store (spec 017). Each test pre-seeds (or leaves empty) a throwaway DB via
/// <see cref="SqliteFindingStore"/>, points the app's <c>Storage:DbPath</c> config at that file, and
/// asserts what <c>GET /api/findings</c> serves back.
/// </summary>
/// <remarks>
/// Oracles are hand-computed from the seed data:
/// <list type="bullet">
///   <item>Three findings seeded with DiscoveredAt of +11h, +10h, +9h are returned most-recent
///   DiscoveredAt first — i.e. 11h, 10h, 9h — regardless of insert order.</item>
///   <item>A fresh DB file the store has only schema-initialised (never written) yields an empty
///   array, exercising the "missing/empty DB → [] (no crash)" contract.</item>
/// </list>
/// </remarks>
public sealed class FindingsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset Anchor =
        new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A WebApplicationFactory that overrides <c>Storage:DbPath</c> to a given temp file. The
    /// override goes through <see cref="IHostBuilder.ConfigureHostConfiguration"/> so it is in place
    /// before <c>Program</c> reads <c>builder.Configuration["Storage:DbPath"]</c> at build time
    /// (app-configuration sources added later would be too late in the minimal-hosting model).
    /// </summary>
    private sealed class TempDbFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath;

        public TempDbFactory(string dbPath) => _dbPath = dbPath;

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:DbPath"] = _dbPath,
                });
            });
            return base.CreateHost(builder);
        }
    }

    /// <summary>Creates a unique temp DB path (the file itself is created by the store on init).</summary>
    private static string NewTempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"ametek-watch-test-{Guid.NewGuid():N}.db");

    private static TriagedFinding MakeFinding(string url, FindingCategory category, int discoveredHour)
    {
        var finding = new Finding(
            Url: url,
            Title: $"Title for {url}",
            Snippet: $"Snippet for {url}",
            PublishedAt: null,
            Category: category,
            DiscoveredAt: Anchor.AddHours(discoveredHour));
        var verdict = new TriageVerdict(
            Important: true,
            Relevant: true,
            WorthReporting: category != FindingCategory.Other,
            Rationale: $"Rationale for {url}");
        return new TriagedFinding(finding, verdict);
    }

    private async Task<IReadOnlyList<TriagedFinding>> GetFindingsAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/findings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var findings = await response.Content.ReadFromJsonAsync<List<TriagedFinding>>(JsonOptions);
        Assert.NotNull(findings);
        return findings!;
    }

    [Fact]
    public async Task GetFindings_ReturnsSeededFindings_MostRecentDiscoveredFirst()
    {
        var dbPath = NewTempDbPath();
        try
        {
            // Seed three findings out of DiscoveredAt order to prove the endpoint, not the insert
            // order, decides the result order.
            var store = new SqliteFindingStore(dbPath);
            await store.SaveAsync(MakeFinding("https://example.com/a", FindingCategory.OpinionSocial, 9), CancellationToken.None);
            await store.SaveAsync(MakeFinding("https://example.com/c", FindingCategory.Other, 11), CancellationToken.None);
            await store.SaveAsync(MakeFinding("https://example.com/b", FindingCategory.FinancialReport, 10), CancellationToken.None);

            await using var factory = new TempDbFactory(dbPath);
            var findings = await GetFindingsAsync(factory);

            Assert.Equal(3, findings.Count);

            // Most-recent DiscoveredAt first: 11h (c), 10h (b), 9h (a).
            var urlsInOrder = findings.Select(f => f.Finding.Url).ToArray();
            Assert.Equal(
                new[]
                {
                    "https://example.com/c", // +11h
                    "https://example.com/b", // +10h
                    "https://example.com/a", // +9h
                },
                urlsInOrder);

            // Fields round-trip through the store and the JSON endpoint.
            var first = findings[0];
            Assert.Equal(Anchor.AddHours(11), first.Finding.DiscoveredAt);
            Assert.Equal(FindingCategory.Other, first.Finding.Category);
            Assert.False(first.Verdict.WorthReporting); // Other => not worth reporting in our seed
        }
        finally
        {
            File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task GetFindings_FreshEmptyDb_ReturnsEmptyArray()
    {
        var dbPath = NewTempDbPath();
        try
        {
            // Schema-init only: the store creates the table but never writes a row.
            _ = new SqliteFindingStore(dbPath);

            await using var factory = new TempDbFactory(dbPath);
            var findings = await GetFindingsAsync(factory);

            Assert.Empty(findings);
        }
        finally
        {
            File.Delete(dbPath);
        }
    }
}
