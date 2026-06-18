using AmetekWatch.Core.Model;
using AmetekWatch.Core.Triage;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for <see cref="TriagePromptBuilder"/> and the <see cref="TriageRubric"/> it serves. Every
/// expected value is hand-stated here as an independent oracle — the phrases the rubric must carry,
/// the exact labelled lines the user message must contain, and the null-PublishedAt fallback — not
/// read back from the production source.
/// </summary>
public class TriagePromptBuilderTests
{
    // A fully-populated finding with a known PublishedAt (oracle: every field below is hand-chosen).
    private static Finding SampleWithDate() => new(
        Url: "https://news.example.com/ametek-opinion",
        Title: "Why I am bullish on AMETEK",
        Snippet: "A first-person take on AME's instruments segment.",
        PublishedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
        Category: FindingCategory.OpinionSocial,
        DiscoveredAt: new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero));

    // Same shape but PublishedAt unknown — exercises the null-safe branch.
    private static Finding SampleWithoutDate() => new(
        Url: "https://ir.example.com/ametek-q2",
        Title: "AMETEK Q2 earnings",
        Snippet: "Reputable-institution financial report.",
        PublishedAt: null,
        Category: FindingCategory.FinancialReport,
        DiscoveredAt: new DateTimeOffset(2026, 6, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void SystemPrompt_StatesTheSpecialWeighting()
    {
        var prompt = TriagePromptBuilder.BuildSystemPrompt();

        // The rubric must put SPECIAL WEIGHT on personal/social opinion + reputable financial reports.
        Assert.Contains("SPECIAL WEIGHT", prompt, StringComparison.Ordinal);
        Assert.Contains("opinion", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("social", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("financial report", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemPrompt_DefinesAllThreeVerdictDimensions()
    {
        var prompt = TriagePromptBuilder.BuildSystemPrompt();

        // The three booleans the verdict must carry.
        Assert.Contains("Important", prompt, StringComparison.Ordinal);
        Assert.Contains("Relevant", prompt, StringComparison.Ordinal);
        Assert.Contains("WorthReporting", prompt, StringComparison.Ordinal);
        // And it must ask for a short rationale alongside the booleans.
        Assert.Contains("rationale", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemPrompt_KeepsGeneralAmetekAwareness()
    {
        var prompt = TriagePromptBuilder.BuildSystemPrompt();

        Assert.Contains("AMETEK", prompt, StringComparison.Ordinal);
        Assert.Contains("AME", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUserContent_LabelsEveryFieldOfTheFinding()
    {
        var finding = SampleWithDate();

        var content = TriagePromptBuilder.BuildUserContent(finding);

        // Each field appears on its own labelled line (oracle values from SampleWithDate()).
        Assert.Contains("Category: OpinionSocial", content, StringComparison.Ordinal);
        Assert.Contains("Title: Why I am bullish on AMETEK", content, StringComparison.Ordinal);
        Assert.Contains("Url: https://news.example.com/ametek-opinion", content, StringComparison.Ordinal);
        Assert.Contains("Snippet: A first-person take on AME's instruments segment.", content, StringComparison.Ordinal);
        // ISO-8601 round-trip of the known date.
        Assert.Contains("PublishedAt: 2026-01-02T03:04:05.0000000+00:00", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUserContent_NullPublishedAt_RendersUnknownAndNeverThrows()
    {
        var finding = SampleWithoutDate();

        var content = TriagePromptBuilder.BuildUserContent(finding);

        Assert.Contains("PublishedAt: (unknown)", content, StringComparison.Ordinal);
        // The other fields still render for the null-date finding.
        Assert.Contains("Category: FinancialReport", content, StringComparison.Ordinal);
        Assert.Contains("Title: AMETEK Q2 earnings", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUserContent_IsDeterministic()
    {
        var finding = SampleWithDate();

        var first = TriagePromptBuilder.BuildUserContent(finding);
        var second = TriagePromptBuilder.BuildUserContent(finding);

        Assert.Equal(first, second);
    }

    [Fact]
    public void BuildUserContent_NullFinding_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TriagePromptBuilder.BuildUserContent(null!));
    }
}
