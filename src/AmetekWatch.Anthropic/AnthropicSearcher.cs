using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Core.Search;

namespace AmetekWatch.Anthropic;

/// <summary>
/// The real <see cref="ISearcher"/>: Sonnet 4.6 with the server-side <c>web_search</c> tool, using the
/// 013 query logic and returning a structured JSON list of hits mapped into <see cref="Finding"/>s. It
/// owns no I/O of its own — it composes the pure <see cref="SearchRequestFactory"/> (build the request),
/// the <see cref="IMessagesClient"/> seam reused from 019 (make the call), the pure
/// <see cref="SearchResponseParser"/> (map the JSON back to hits), and the pure 013
/// <see cref="SearchResultMapper"/> (classify each hit into a finding). An injected
/// <see cref="DateTimeOffset"/> provider stamps discovery — there is no <c>DateTimeOffset.Now</c>
/// inside, so the whole class is deterministic and unit-testable offline with a fake client.
/// </summary>
/// <remarks>
/// The live <c>web_search</c> server-tool loop may emit <c>pause_turn</c> / <c>stop_reason</c>
/// continuations as the model runs searches; the offline build does NOT exercise that loop (the fake
/// client returns the final JSON directly). Handling those continuations in the live
/// <see cref="AnthropicMessagesClient"/> is a documented follow-up live-hardening concern.
/// </remarks>
public sealed class AnthropicSearcher : ISearcher
{
    private readonly IMessagesClient _client;
    private readonly SearchRequestFactory _factory;
    private readonly SearchResponseParser _parser;
    private readonly Func<DateTimeOffset> _clock;

    /// <param name="client">The Messages seam (reused from 019); never null.</param>
    /// <param name="factory">Pure request builder; never null.</param>
    /// <param name="parser">Pure response parser; never null.</param>
    /// <param name="clock">
    /// Discovery-time provider — injected so discovery stamps are deterministic and no clock is read
    /// inside this class; never null.
    /// </param>
    public AnthropicSearcher(
        IMessagesClient client,
        SearchRequestFactory factory,
        SearchResponseParser parser,
        Func<DateTimeOffset> clock)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(clock);

        _client = client;
        _factory = factory;
        _parser = parser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = _factory.Build(query);
        var json = await _client.CreateMessageTextAsync(request, ct).ConfigureAwait(false);
        var items = _parser.Parse(json);

        // Stamp every hit with one discovery instant captured for this sweep.
        var discoveredAt = _clock();
        var findings = new List<Finding>(items.Count);
        foreach (var item in items)
        {
            findings.Add(SearchResultMapper.ToFinding(item, discoveredAt));
        }

        return findings;
    }
}
