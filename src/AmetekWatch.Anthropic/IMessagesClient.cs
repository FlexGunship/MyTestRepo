using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Narrow seam over the Anthropic Messages endpoint: send a fully-built request, get back the
/// text of the response's first content block. The whole point of the seam is testability — the
/// triage decider depends on this interface, never on the concrete SDK client, so the decider,
/// request factory, and parser are all exercised offline with a deterministic fake. The single
/// untested line in this assembly is <see cref="AnthropicMessagesClient"/>, which wraps the live
/// SDK call this interface abstracts.
/// </summary>
public interface IMessagesClient
{
    /// <summary>
    /// Sends <paramref name="parameters"/> to the model and returns the text of the first text
    /// block in the response.
    /// </summary>
    Task<string> CreateMessageTextAsync(MessageCreateParams parameters, CancellationToken ct);
}
