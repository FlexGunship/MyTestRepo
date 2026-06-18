using System.Text.Json;
using AmetekWatch.Anthropic;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Triage;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Hand-computed oracles for <see cref="TriageRequestFactory"/>: the request must pin the Opus 4.8
/// model id, carry the rubric as a cache-controlled system block, expose the four-field structured
/// schema, and render the finding verbatim through <see cref="TriagePromptBuilder.BuildUserContent"/>.
/// </summary>
public class TriageRequestFactoryTests
{
    private static Finding SampleFinding() => new(
        Url: "https://example.com/ame-oped",
        Title: "Why AMETEK's culture worries this shareholder",
        Snippet: "A personal take on AME's acquisition spree.",
        PublishedAt: new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero),
        Category: FindingCategory.OpinionSocial,
        DiscoveredAt: new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Build_PinsOpus48ModelId()
    {
        var request = new TriageRequestFactory().Build(SampleFinding());

        Assert.Contains("claude-opus-4-8", request.Model.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SystemBlockCarriesRubricTextAndCacheControl()
    {
        var request = new TriageRequestFactory().Build(SampleFinding());

        Assert.True(request.System!.TryPickTextBlockParams(out var blocks));
        var block = Assert.Single(blocks!);
        Assert.Equal(TriagePromptBuilder.BuildSystemPrompt(), block.Text);
        Assert.NotNull(block.CacheControl);
    }

    [Fact]
    public void Build_SchemaHasTheFourRequiredVerdictFields()
    {
        var request = new TriageRequestFactory().Build(SampleFinding());

        var schema = request.OutputConfig!.Format!.Schema;

        // additionalProperties:false, object type.
        Assert.Equal("object", schema["type"].GetString());
        Assert.False(schema["additionalProperties"].GetBoolean());

        // The four properties are all present.
        var properties = schema["properties"];
        foreach (var name in new[] { "important", "relevant", "worthReporting", "rationale" })
        {
            Assert.True(properties.TryGetProperty(name, out _), $"schema missing property '{name}'");
        }

        // All four are required.
        var required = schema["required"].EnumerateArray().Select(e => e.GetString()).ToHashSet();
        Assert.Equal(
            new HashSet<string?> { "important", "relevant", "worthReporting", "rationale" },
            required);
    }

    [Fact]
    public void Build_UserContentEqualsBuildUserContent()
    {
        var finding = SampleFinding();
        var request = new TriageRequestFactory().Build(finding);

        var message = Assert.Single(request.Messages);
        Assert.True(message.Content.TryPickString(out var text));
        Assert.Equal(TriagePromptBuilder.BuildUserContent(finding), text);
    }

    [Fact]
    public void Build_NullFinding_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TriageRequestFactory().Build(null!));
    }
}
