using System.Text;
using System.Text.Json;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Search;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Pure builder that turns a <see cref="SweepQuery"/> into a fully-formed <see cref="MessageCreateParams"/>
/// for the Sonnet 4.6 searcher call — no I/O, no model call. It composes the 013 query logic
/// (<see cref="SearchQueryBuilder"/>) with the SDK's request shape:
/// <list type="bullet">
///   <item>Model <c>claude-sonnet-4-6</c>.</item>
///   <item>The server-side <c>web_search</c> tool (<see cref="WebSearchTool20260209"/>) so the model can
///   actually reach the live web; the offline build never exercises this.</item>
///   <item>A user message that lists the 013 <see cref="SearchQueryBuilder.BuildQueries"/> terms and asks
///   the model to return ONLY a JSON array of hits.</item>
///   <item>Structured output: a JSON-array schema pinning the five item fields
///   (<c>url, title, snippet, publishedAt, sourceDomain</c>).</item>
/// </list>
/// </summary>
/// <remarks>
/// The live <c>web_search</c> server-tool loop may emit <c>pause_turn</c> / <c>stop_reason</c>
/// continuations as the model runs searches. The offline build does NOT exercise that loop — the fake
/// <see cref="IMessagesClient"/> returns the final JSON directly. A continuation loop in the live
/// <see cref="AnthropicMessagesClient"/> is a follow-up live-hardening concern, out of scope here.
/// </remarks>
public sealed class SearchRequestFactory
{
    /// <summary>
    /// Generous output cap — web search returns many large result blocks plus the final JSON array.
    /// </summary>
    private const long MaxTokens = 8192;

    /// <summary>
    /// Builds the search request for <paramref name="query"/>. Pure: same query in, byte-identical
    /// params out. Throws <see cref="ArgumentNullException"/> if <paramref name="query"/> is null.
    /// </summary>
    public MessageCreateParams Build(SweepQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = MaxTokens,
            Tools = new List<ToolUnion> { new WebSearchTool20260209() },
            Messages = new List<MessageParam>
            {
                new()
                {
                    Role = Role.User,
                    Content = BuildUserContent(query),
                },
            },
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = BuildSchema() },
            },
        };
    }

    /// <summary>
    /// The user message: instructs the model to run the 013 queries via web search and return ONLY a
    /// JSON array of hits. Deterministic — the 013 query terms are rendered in their fixed order.
    /// </summary>
    private static string BuildUserContent(SweepQuery query)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Use the web_search tool to find fresh, relevant material for each of the search queries below.");
        sb.AppendLine("Search queries:");
        foreach (var q in SearchQueryBuilder.BuildQueries(query))
        {
            sb.Append("- ").AppendLine(q);
        }

        sb.AppendLine();
        sb.AppendLine(
            "Return ONLY a JSON array of result objects — no prose, no markdown. Each object has exactly:");
        sb.AppendLine("  - url: the canonical source URL (string)");
        sb.AppendLine("  - title: the headline or page title (string)");
        sb.AppendLine("  - snippet: a short excerpt or summary (string)");
        sb.AppendLine("  - publishedAt: ISO-8601 timestamp the source was published, or null");
        sb.AppendLine("  - sourceDomain: the host the result came from (string), or null");
        return sb.ToString();
    }

    /// <summary>
    /// The structured-output schema: a JSON array whose items are
    /// <c>{ url, title, snippet : string, publishedAt : string|null, sourceDomain : string|null }</c>
    /// with <c>url, title, snippet</c> required and <c>additionalProperties:false</c>.
    /// </summary>
    private static Dictionary<string, JsonElement> BuildSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("array"),
        ["items"] = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                url = new { type = "string" },
                title = new { type = "string" },
                snippet = new { type = "string" },
                publishedAt = new { type = new[] { "string", "null" } },
                sourceDomain = new { type = new[] { "string", "null" } },
            },
            required = new[] { "url", "title", "snippet", "publishedAt", "sourceDomain" },
            additionalProperties = false,
        }),
    };
}
