using Anthropic.Exceptions;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Classifies an exception thrown by the Anthropic pipeline as <i>transient</i> (worth retrying) or
/// not, for use as the <c>shouldRetry</c> predicate of
/// <see cref="AmetekWatch.Core.Resilience.RetryPolicy"/>. Deliberately <b>conservative</b>: it
/// retries only failures we are confident are transient (a momentary server/network condition that a
/// later attempt could succeed past); everything else — bad requests, auth/permission failures,
/// not-found, malformed-response parse errors, plain argument bugs — is treated as permanent so we
/// fail fast rather than hammering the API with a request that can never succeed.
/// </summary>
/// <remarks>
/// Exception types come from the official Anthropic .NET SDK (NuGet <c>Anthropic</c>, namespace
/// <c>Anthropic.Exceptions</c>). The relevant hierarchy:
/// <list type="bullet">
///   <item><c>AnthropicApiException</c> — base of all HTTP-status errors; carries
///   <c>HttpStatusCode StatusCode</c>. Its subtypes: <c>Anthropic4xxException</c>
///   (<c>AnthropicBadRequestException</c> 400, <c>AnthropicUnauthorizedException</c> 401,
///   <c>AnthropicForbiddenException</c> 403, <c>AnthropicNotFoundException</c> 404,
///   <c>AnthropicRateLimitException</c> 429, <c>AnthropicUnprocessableEntityException</c> 422),
///   <c>Anthropic5xxException</c> (server errors, including overloaded 529), and
///   <c>AnthropicUnexpectedStatusCodeException</c>.</item>
///   <item><c>AnthropicIOException</c> — a network/transport failure wrapping an
///   <see cref="System.Net.Http.HttpRequestException"/>.</item>
///   <item><c>AnthropicInvalidDataException</c> — a malformed/unexpected response (parse-time);
///   non-transient.</item>
/// </list>
/// Transient = rate-limit (HTTP 429), overloaded (529), any 5xx server error, and
/// network/timeout failures (<see cref="System.Net.Http.HttpRequestException"/>,
/// <see cref="System.Threading.Tasks.TaskCanceledException"/>/<see cref="TimeoutException"/> — an
/// <c>HttpClient</c> request timeout surfaces as a <c>TaskCanceledException</c>). Non-API,
/// argument, and parse errors are <b>not</b> transient.
/// </remarks>
public static class AnthropicTransient
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="ex"/> is a clearly-transient failure that a retry
    /// could succeed past; <c>false</c> for permanent/unknown failures (the conservative default).
    /// </summary>
    public static bool IsTransient(Exception? ex)
    {
        if (ex is null)
        {
            return false;
        }

        // Anthropic SDK HTTP errors. Match the transient subclasses by type first — rate-limit (429)
        // and every 5xx (including overloaded 529) are transient regardless of how StatusCode is
        // populated. Then fall back to the StatusCode on the base type, which covers the
        // unexpected-status subclass; the remaining 4xx subclasses (400/401/403/404/422) fall through
        // here with a < 500, non-429 status and are correctly classed non-transient (a retry can't
        // fix a bad/auth/not-found request). 529 has no named HttpStatusCode member, but its integer
        // value round-trips through the cast.
        if (ex is AnthropicRateLimitException or Anthropic5xxException)
        {
            return true;
        }

        if (ex is AnthropicApiException api)
        {
            var status = (int)api.StatusCode;
            return status == 429 || status >= 500;
        }

        // Anthropic SDK transport failure (wraps an HttpRequestException: connection reset, DNS, etc.).
        if (ex is AnthropicIOException)
        {
            return true;
        }

        // Raw network / timeout failures (also when not surfaced through the SDK wrapper). An
        // HttpClient request timeout is delivered as a TaskCanceledException.
        if (ex is System.Net.Http.HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return true;
        }

        // Everything else — argument errors (ArgumentException et al.), parse/format errors
        // (FormatException), AnthropicInvalidDataException, and any other non-API exception — is
        // treated as non-transient.
        return false;
    }
}
