using AmetekWatch.Anthropic;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Deterministic stand-in for <see cref="IMessagesClient"/>: returns a fixed JSON string and
/// records the request it was handed, so a test can drive <see cref="AnthropicTriageDecider"/>
/// end-to-end with no network and assert both the response mapping and (if needed) the request.
/// </summary>
internal sealed class FakeMessagesClient : IMessagesClient
{
    private readonly string _cannedJson;

    public FakeMessagesClient(string cannedJson) => _cannedJson = cannedJson;

    /// <summary>The most recent request passed to <see cref="CreateMessageTextAsync"/>.</summary>
    public MessageCreateParams? LastRequest { get; private set; }

    public Task<string> CreateMessageTextAsync(MessageCreateParams parameters, CancellationToken ct)
    {
        LastRequest = parameters;
        return Task.FromResult(_cannedJson);
    }
}
