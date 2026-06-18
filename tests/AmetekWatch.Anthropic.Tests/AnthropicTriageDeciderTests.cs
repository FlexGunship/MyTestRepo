using AmetekWatch.Anthropic;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// End-to-end (offline) wiring of <see cref="AnthropicTriageDecider"/>: with a fake messages client
/// returning canned structured JSON, the decider builds a request, hands it to the client, and parses
/// the response into the expected <see cref="TriageVerdict"/>. No network, no key.
/// </summary>
public class AnthropicTriageDeciderTests
{
    private static Finding SampleFinding() => new(
        Url: "https://example.com/ame-earnings",
        Title: "AMETEK Q2 earnings beat expectations",
        Snippet: "Revenue and EPS above consensus.",
        PublishedAt: new DateTimeOffset(2026, 6, 12, 13, 30, 0, TimeSpan.Zero),
        Category: FindingCategory.FinancialReport,
        DiscoveredAt: new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task JudgeAsync_WithCannedJson_YieldsExpectedVerdict()
    {
        const string cannedJson =
            """{"important":true,"relevant":true,"worthReporting":true,"rationale":"Reputable earnings report."}""";
        var fake = new FakeMessagesClient(cannedJson);
        ITriageDecider decider = new AnthropicTriageDecider(
            fake, new TriageRequestFactory(), new TriageVerdictParser());

        var verdict = await decider.JudgeAsync(SampleFinding(), CancellationToken.None);

        Assert.Equal(
            new TriageVerdict(
                Important: true,
                Relevant: true,
                WorthReporting: true,
                Rationale: "Reputable earnings report."),
            verdict);
    }

    [Fact]
    public async Task JudgeAsync_SendsAnOpus48RequestToTheClient()
    {
        const string cannedJson =
            """{"important":false,"relevant":true,"worthReporting":false,"rationale":"Off-topic."}""";
        var fake = new FakeMessagesClient(cannedJson);
        var decider = new AnthropicTriageDecider(
            fake, new TriageRequestFactory(), new TriageVerdictParser());

        await decider.JudgeAsync(SampleFinding(), CancellationToken.None);

        Assert.NotNull(fake.LastRequest);
        Assert.Contains("claude-opus-4-8", fake.LastRequest!.Model.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task JudgeAsync_NullFinding_Throws()
    {
        var decider = new AnthropicTriageDecider(
            new FakeMessagesClient("{}"), new TriageRequestFactory(), new TriageVerdictParser());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => decider.JudgeAsync(null!, CancellationToken.None));
    }
}
