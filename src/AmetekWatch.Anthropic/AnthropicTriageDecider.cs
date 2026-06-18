using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.Anthropic;

/// <summary>
/// The real <see cref="ITriageDecider"/>: Opus 4.8 judging a <see cref="Finding"/> against the 011
/// rubric with structured output. It owns no I/O of its own — it composes three injected
/// collaborators: the pure <see cref="TriageRequestFactory"/> (build the request), the
/// <see cref="IMessagesClient"/> seam (make the call), and the pure <see cref="TriageVerdictParser"/>
/// (map the JSON back to a verdict). With a fake messages client this whole class is unit-testable
/// offline; in production the client is <see cref="AnthropicMessagesClient"/>.
/// </summary>
public sealed class AnthropicTriageDecider : ITriageDecider
{
    private readonly IMessagesClient _client;
    private readonly TriageRequestFactory _factory;
    private readonly TriageVerdictParser _parser;

    public AnthropicTriageDecider(
        IMessagesClient client,
        TriageRequestFactory factory,
        TriageVerdictParser parser)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(parser);

        _client = client;
        _factory = factory;
        _parser = parser;
    }

    public async Task<TriageVerdict> JudgeAsync(Finding finding, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var request = _factory.Build(finding);
        var json = await _client.CreateMessageTextAsync(request, ct).ConfigureAwait(false);
        return _parser.Parse(json);
    }
}
