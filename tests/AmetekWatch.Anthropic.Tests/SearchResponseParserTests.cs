using AmetekWatch.Anthropic;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Hand-computed oracles for <see cref="SearchResponseParser"/>: a known JSON array maps field-by-field
/// onto <c>SearchResultItem</c>s (including null <c>publishedAt</c> / <c>sourceDomain</c>), an empty
/// array yields an empty list, and malformed / wrong-shaped JSON throws <see cref="FormatException"/>.
/// </summary>
public class SearchResponseParserTests
{
    private const string TwoHits = """
        [
          {
            "url": "https://sec.gov/edgar/ame-10q",
            "title": "AMETEK Q2 earnings",
            "snippet": "Quarterly results.",
            "publishedAt": "2026-06-10T09:00:00+00:00",
            "sourceDomain": "sec.gov"
          },
          {
            "url": "https://blog.example.com/ame-take",
            "title": "An opinion on AME",
            "snippet": "A personal take.",
            "publishedAt": null,
            "sourceDomain": null
          }
        ]
        """;

    [Fact]
    public void Parse_KnownArray_MapsEveryField()
    {
        var items = new SearchResponseParser().Parse(TwoHits);

        Assert.Equal(2, items.Count);

        var first = items[0];
        Assert.Equal("https://sec.gov/edgar/ame-10q", first.Url);
        Assert.Equal("AMETEK Q2 earnings", first.Title);
        Assert.Equal("Quarterly results.", first.Snippet);
        Assert.Equal(new DateTimeOffset(2026, 6, 10, 9, 0, 0, TimeSpan.Zero), first.PublishedAt);
        Assert.Equal("sec.gov", first.SourceDomain);

        var second = items[1];
        Assert.Equal("https://blog.example.com/ame-take", second.Url);
        Assert.Null(second.PublishedAt);
        Assert.Null(second.SourceDomain);
    }

    [Fact]
    public void Parse_EmptyArray_YieldsEmptyList()
    {
        var items = new SearchResponseParser().Parse("[]");

        Assert.Empty(items);
    }

    [Fact]
    public void Parse_Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => new SearchResponseParser().Parse("not json at all"));
    }

    [Fact]
    public void Parse_ObjectRootNotArray_Throws()
    {
        Assert.Throws<FormatException>(() => new SearchResponseParser().Parse("""{"url":"x"}"""));
    }

    [Fact]
    public void Parse_MissingRequiredField_Throws()
    {
        // Element has no "title".
        Assert.Throws<FormatException>(() => new SearchResponseParser().Parse(
            """[{"url":"https://x","snippet":"s","publishedAt":null,"sourceDomain":null}]"""));
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SearchResponseParser().Parse(null!));
    }
}
