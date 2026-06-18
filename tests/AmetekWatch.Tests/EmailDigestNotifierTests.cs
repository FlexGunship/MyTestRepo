using AmetekWatch.Core.Model;
using AmetekWatch.Core.Notify;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for the email digest sink (<see cref="EmailDigestNotifier"/>) and the shared
/// <see cref="DigestMarkdownRenderer"/> it composes the body with. Every expected value is
/// hand-computed here, independent of the renderer's own source.
///
/// A fake <see cref="IEmailSender"/> captures the subject + body that would be sent, so no live
/// SMTP is exercised. The run time is always injected (never an ambient clock); the +02:00 stamp
/// must normalise to <c>14:30 UTC</c>, proving the renderer converts to universal time.
/// </summary>
public sealed class EmailDigestNotifierTests
{
    /// <summary>Records the single send so the test can assert on subject + body.</summary>
    private sealed class FakeEmailSender : IEmailSender
    {
        public int Sends { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }

        public Task SendAsync(string subject, string body, CancellationToken ct)
        {
            Sends++;
            Subject = subject;
            Body = body;
            return Task.CompletedTask;
        }
    }

    private static EmailOptions Options() => new(
        Enabled: true,
        SmtpHost: "smtp.example.com",
        SmtpPort: 587,
        From: "watch@example.com",
        To: new[] { "investor@example.com" },
        SubjectPrefix: "AMETEK Watch");

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
    public async Task SendsFriendlySubjectAndRenderedBody_ForSeededDigest()
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

        var sender = new FakeEmailSender();
        // 16:30 at +02:00 == 14:30 UTC — exercises the universal-time conversion.
        var stamp = new DateTimeOffset(2026, 6, 18, 16, 30, 0, TimeSpan.FromHours(2));
        var notifier = new EmailDigestNotifier(
            sender, Options(), new DigestMarkdownRenderer(), "AMETEK", () => stamp);

        await notifier.NotifyAsync(digest, CancellationToken.None);

        Assert.Equal(1, sender.Sends);
        Assert.Equal("AMETEK Watch — 2 findings worth reporting", sender.Subject);

        var body = sender.Body!.Replace("\r\n", "\n");

        // The body is exactly the shared renderer's friendly Markdown.
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

        Assert.Equal(expected, body);

        // Friendly names only — no internal type/property/enum identifiers leak into subject or body.
        foreach (var leak in new[]
        {
            "OpinionSocial", "FinancialReport", "WorthReporting", "Rationale",
            "DiscoveredAt", "TriagedFinding", "Verdict", "Category",
        })
        {
            Assert.DoesNotContain(leak, body);
            Assert.DoesNotContain(leak, sender.Subject);
        }
    }

    [Fact]
    public async Task SendsNothingToReportEmail_ForEmptyDigest()
    {
        var sender = new FakeEmailSender();
        var stamp = new DateTimeOffset(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);
        var notifier = new EmailDigestNotifier(
            sender, Options(), new DigestMarkdownRenderer(), "AMETEK", () => stamp);

        await notifier.NotifyAsync(new List<TriagedFinding>(), CancellationToken.None);

        Assert.Equal(1, sender.Sends);
        Assert.Equal("AMETEK Watch — nothing to report", sender.Subject);

        var body = sender.Body!.Replace("\r\n", "\n");

        const string expected =
            "# AMETEK Watch digest\n" +
            "\n" +
            "_Generated Thursday, 18 June 2026 14:30 UTC_\n" +
            "\n" +
            "Nothing to report this run.\n";

        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task UsesSingularNoun_ForOneFinding()
    {
        var digest = new List<TriagedFinding>
        {
            Worth(
                "https://news.example.com/ametek-analyst-note",
                "AMETEK shares climb on upbeat analyst note",
                FindingCategory.OpinionSocial,
                "Personal investor commentary."),
        };

        var sender = new FakeEmailSender();
        var stamp = new DateTimeOffset(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);
        var notifier = new EmailDigestNotifier(
            sender, Options(), new DigestMarkdownRenderer(), "AMETEK", () => stamp);

        await notifier.NotifyAsync(digest, CancellationToken.None);

        Assert.Equal("AMETEK Watch — 1 finding worth reporting", sender.Subject);
    }

    [Fact]
    public void RendererRendersExpectedMarkdown()
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
        var actual = new DigestMarkdownRenderer()
            .Render(digest, "AMETEK", stamp)
            .Replace("\r\n", "\n");

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
    }

    [Fact]
    public void RendererRendersNothingToReport_ForEmptyDigest()
    {
        var stamp = new DateTimeOffset(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);
        var actual = new DigestMarkdownRenderer()
            .Render(new List<TriagedFinding>(), "AMETEK", stamp)
            .Replace("\r\n", "\n");

        const string expected =
            "# AMETEK Watch digest\n" +
            "\n" +
            "_Generated Thursday, 18 June 2026 14:30 UTC_\n" +
            "\n" +
            "Nothing to report this run.\n";

        Assert.Equal(expected, actual);
    }
}
