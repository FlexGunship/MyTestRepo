using AmetekWatch.Anthropic;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Search;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Hand-computed oracles for <see cref="SearchRequestFactory"/>: the request must pin the Sonnet 4.6
/// model id, carry the server-side <c>web_search</c> tool, render every 013
/// <see cref="SearchQueryBuilder.BuildQueries"/> term into the user prompt, and expose the five-field
/// JSON-array structured-output schema.
/// </summary>
public class SearchRequestFactoryTests
{
    private static SweepQuery SampleQuery() => new(Subject: "AMETEK");

    [Fact]
    public void Build_PinsSonnet46ModelId()
    {
        var request = new SearchRequestFactory().Build(SampleQuery());

        Assert.Contains("claude-sonnet-4-6", request.Model.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Build_IncludesWebSearchTool()
    {
        var request = new SearchRequestFactory().Build(SampleQuery());

        var tool = Assert.Single(request.Tools!);
        Assert.True(tool.TryPickWebSearchTool20260209(out _));
    }

    [Fact]
    public void Build_UserPromptCarriesEvery013QueryTerm()
    {
        var query = SampleQuery();
        var request = new SearchRequestFactory().Build(query);

        var message = Assert.Single(request.Messages);
        Assert.True(message.Content.TryPickString(out var text));

        // Every query the 013 builder produces must appear verbatim in the prompt.
        foreach (var q in SearchQueryBuilder.BuildQueries(query))
        {
            Assert.Contains(q, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Build_UserPromptAsksForJsonArrayOfHits()
    {
        var request = new SearchRequestFactory().Build(SampleQuery());

        var message = Assert.Single(request.Messages);
        Assert.True(message.Content.TryPickString(out var text));

        Assert.Contains("web_search", text, StringComparison.Ordinal);
        Assert.Contains("JSON array", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SchemaIsArrayOfTheFiveItemFields()
    {
        var request = new SearchRequestFactory().Build(SampleQuery());

        var schema = request.OutputConfig!.Format!.Schema;

        Assert.Equal("array", schema["type"].GetString());

        var itemProps = schema["items"].GetProperty("properties");
        foreach (var name in new[] { "url", "title", "snippet", "publishedAt", "sourceDomain" })
        {
            Assert.True(itemProps.TryGetProperty(name, out _), $"item schema missing property '{name}'");
        }
    }

    [Fact]
    public void Build_NullQuery_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SearchRequestFactory().Build(null!));
    }
}
