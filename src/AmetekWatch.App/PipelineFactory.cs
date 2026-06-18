using AmetekWatch.Anthropic;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.App;

/// <summary>
/// Resolves the (<see cref="ISearcher"/>, <see cref="ITriageDecider"/>) pair the host runs: the real
/// Anthropic adapters (Sonnet&#8201;4.6 search → Opus&#8201;4.8 triage) when <c>useRealApi</c> is
/// <c>true</c>, the deterministic Core fakes otherwise.
/// </summary>
/// <remarks>
/// Type selection only — this helper builds objects but invokes nothing, so it is exercised offline
/// with no network and no key (the test asserts the resolved runtime types directly). The
/// env-key check and warn-and-fall-back live in <c>Program</c>, deliberately kept out of here so the
/// real path can be resolved and type-asserted without a key.
/// </remarks>
public static class PipelineFactory
{
    /// <summary>
    /// Builds the searcher/triage pair. When <paramref name="useRealApi"/> is <c>true</c>,
    /// <paramref name="realClientFactory"/> supplies the live <see cref="IMessagesClient"/> (shared by
    /// both adapters) and <paramref name="clock"/> stamps discovery (defaults to
    /// <c>() =&gt; DateTimeOffset.UtcNow</c>); both are ignored for the fake pair.
    /// </summary>
    public static (ISearcher Searcher, ITriageDecider Triage) Create(
        bool useRealApi,
        Func<IMessagesClient>? realClientFactory = null,
        Func<DateTimeOffset>? clock = null)
    {
        if (!useRealApi)
        {
            return (new FakeSearcher(), new FakeTriageDecider());
        }

        ArgumentNullException.ThrowIfNull(realClientFactory);
        var client = realClientFactory();
        var resolvedClock = clock ?? (() => DateTimeOffset.UtcNow);

        var searcher = new AnthropicSearcher(
            client,
            new SearchRequestFactory(),
            new SearchResponseParser(),
            resolvedClock);
        var triage = new AnthropicTriageDecider(
            client,
            new TriageRequestFactory(),
            new TriageVerdictParser());

        return (searcher, triage);
    }
}
