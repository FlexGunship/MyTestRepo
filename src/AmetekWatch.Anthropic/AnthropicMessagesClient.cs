using System.Linq;
using Anthropic.Models.Messages;
using AnthropicSdk = Anthropic;

namespace AmetekWatch.Anthropic;

/// <summary>
/// The one line of this assembly that talks to the network: wraps the official SDK's
/// <see cref="AnthropicSdk.AnthropicClient"/> (which reads <c>ANTHROPIC_API_KEY</c> from the
/// environment) and returns the first text block of the response. Deliberately kept tiny — it is
/// the only code here that is NOT unit-tested, since exercising it requires a live API key. All
/// behaviour worth testing lives behind <see cref="IMessagesClient"/> in pure, faked code.
/// </summary>
public sealed class AnthropicMessagesClient : IMessagesClient
{
    private readonly AnthropicSdk.AnthropicClient _client = new();

    public async Task<string> CreateMessageTextAsync(MessageCreateParams parameters, CancellationToken ct)
    {
        var response = await _client.Messages.Create(parameters, ct).ConfigureAwait(false);
        return response.Content.Select(b => b.Value).OfType<TextBlock>().First().Text;
    }
}
