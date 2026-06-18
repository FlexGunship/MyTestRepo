using AmetekWatch.Core.Model;
using AmetekWatch.Core.Notify;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for the digest-delivery seam (<see cref="IDigestNotifier"/>) and its two slice
/// implementations. Every expected value is hand-computed here, independent of the renderer.
///
/// The run time is always injected (never an ambient clock), so the output is deterministic.
/// All inputs use a fixed timestamp; <c>2026-06-18</c> is a Thursday, and the +02:00 variant in
/// <see cref="WritesFriendlyMarkdown_ForSeededDigest"/> must normalise to <c>14:30 UTC</c>,
/// proving the renderer converts to universal time.
/// </summary>
public sealed class DigestNotifierTests : IDisposable
{
    // A unique temp file per test instance.
    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"ametek-digest-test-{Guid.NewGuid():N}.md");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private static TriagedFinding Worth(
        string url, string title, FindingCategory category, string rationale) =>
        new(
            new Finding(
                Url: url,
                Title: title,
                Snippet: "snippet",
                PublishedAt: null,
                Category: category,
                DiscoveredAt: new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero)),
            new TriageVerdict(
                Important: true,
                Relevant: true,
                WorthReporting: true,
                Rationale: rationale));

    [Fact]
    public async Task WritesFriendlyMarkdown_ForSeededDigest()
    {
        var digest = new List<TriagedFinding>
        {
            Worth(
                "https://news.example.com/ametek-analyst-note",
                "AMETEK shares climb on upbeat analyst note",
                FindingCategory.OpinionSocial,
                "Personal investor commentary on AMETEK's guidance."),
            Worth(
                "https://ir.example.com/ametek-q2-earnings",
                "AMETEK reports Q2 earnings beat",
                FindingCategory.FinancialReport,
                "Reputable quarterly results from the company's investor relations."),
        };

        // 16:30 at +02:00 == 14:30 UTC — exercises the universal-time conversion.
        var stamp = new DateTimeOffset(2026, 6, 18, 16, 30, 0, TimeSpan.FromHours(2));
        var notifier = new FileDigestNotifier(_path, "AMETEK", () => stamp);

        await notifier.NotifyAsync(digest, CancellationToken.None);

        var actual = (await File.ReadAllTextAsync(_path)).Replace("\r\n", "\n");

        const string expected =
            "# AMETEK Watch digest\n" +
            "\n" +
            "_Generated Thursday, 18 June 2026 14:30 UTC_\n" +
            "\n" +
            "**2 items worth reporting.**\n" +
            "\n" +
            "## Opinion / Social: AMETEK shares climb on upbeat analyst note\n" +
            "\n" +
            "- Link: https://news.example.com/ametek-analyst-note\n" +
            "- Why it matters: Personal investor commentary on AMETEK's guidance.\n" +
            "\n" +
            "## Financial Report: AMETEK reports Q2 earnings beat\n" +
            "\n" +
            "- Link: https://ir.example.com/ametek-q2-earnings\n" +
            "- Why it matters: Reputable quarterly results from the company's investor relations.\n" +
            "\n";

        Assert.Equal(expected, actual);

        // Friendly names only — no internal type/property/enum identifiers leak in.
        foreach (var leak in new[]
        {
            "OpinionSocial", "FinancialReport", "WorthReporting", "Rationale",
            "DiscoveredAt", "TriagedFinding", "Verdict", "Url:", "Category",
        })
        {
            Assert.DoesNotContain(leak, actual);
        }
    }

    [Fact]
    public async Task WritesNothingToReport_ForEmptyDigest()
    {
        var stamp = new DateTimeOffset(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);
        var notifier = new FileDigestNotifier(_path, "AMETEK", () => stamp);

        await notifier.NotifyAsync(new List<TriagedFinding>(), CancellationToken.None);

        var actual = (await File.ReadAllTextAsync(_path)).Replace("\r\n", "\n");

        const string expected =
            "# AMETEK Watch digest\n" +
            "\n" +
            "_Generated Thursday, 18 June 2026 14:30 UTC_\n" +
            "\n" +
            "Nothing to report this run.\n";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OverwritesFile_OnEachRun()
    {
        var stamp = new DateTimeOffset(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);
        var notifier = new FileDigestNotifier(_path, "AMETEK", () => stamp);

        var seeded = new List<TriagedFinding>
        {
            Worth(
                "https://news.example.com/ametek-analyst-note",
                "AMETEK shares climb on upbeat analyst note",
                FindingCategory.OpinionSocial,
                "Personal investor commentary."),
        };

        await notifier.NotifyAsync(seeded, CancellationToken.None);
        await notifier.NotifyAsync(new List<TriagedFinding>(), CancellationToken.None);

        var actual = (await File.ReadAllTextAsync(_path)).Replace("\r\n", "\n");

        // The second (empty) run fully replaces the first — no stale items linger.
        Assert.Contains("Nothing to report this run.", actual);
        Assert.DoesNotContain("analyst note", actual);
    }

    [Fact]
    public async Task NullNotifier_WritesNothing()
    {
        var notifier = new NullDigestNotifier();
        var digest = new List<TriagedFinding>
        {
            Worth(
                "https://news.example.com/ametek-analyst-note",
                "AMETEK shares climb on upbeat analyst note",
                FindingCategory.OpinionSocial,
                "Personal investor commentary."),
        };

        await notifier.NotifyAsync(digest, CancellationToken.None);

        Assert.False(File.Exists(_path));
    }
}
