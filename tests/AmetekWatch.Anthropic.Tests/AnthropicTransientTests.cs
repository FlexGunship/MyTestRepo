using System.Net;
using System.Net.Http;
using AmetekWatch.Anthropic;
using Anthropic.Exceptions;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Oracles for <see cref="AnthropicTransient.IsTransient(Exception?)"/> — the <c>shouldRetry</c>
/// predicate for the real-pipeline retry policy. Covers the cases we can construct offline: the
/// transient Anthropic SDK API subclasses (rate-limit, 5xx), a non-transient API subclass
/// (bad-request), the network/timeout exceptions, plain argument/parse errors, and null. No network.
/// </summary>
public class AnthropicTransientTests
{
    private static HttpRequestException Hre() => new("transport boom");

    [Fact]
    public void RateLimit_429_IsTransient()
    {
        var ex = new AnthropicRateLimitException(Hre())
        {
            StatusCode = (HttpStatusCode)429,
            ResponseBody = "rate limited",
        };

        Assert.True(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void ServerError_5xx_IsTransient()
    {
        var ex = new Anthropic5xxException(Hre())
        {
            StatusCode = (HttpStatusCode)503,
            ResponseBody = "unavailable",
        };

        Assert.True(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void Overloaded_529_IsTransient()
    {
        // 529 (overloaded) has no named HttpStatusCode member; the raw int still classifies as 5xx.
        var ex = new Anthropic5xxException(Hre())
        {
            StatusCode = (HttpStatusCode)529,
            ResponseBody = "overloaded",
        };

        Assert.True(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void UnexpectedStatus_5xx_IsTransient_ViaStatusCode()
    {
        var ex = new AnthropicApiException("gateway timeout", Hre())
        {
            StatusCode = (HttpStatusCode)504,
            ResponseBody = "gateway timeout",
        };

        Assert.True(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void BadRequest_400_IsNotTransient()
    {
        var ex = new AnthropicBadRequestException(Hre())
        {
            StatusCode = HttpStatusCode.BadRequest,
            ResponseBody = "bad request",
        };

        Assert.False(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void OtherApiError_4xx_IsNotTransient_ViaStatusCode()
    {
        var ex = new AnthropicApiException("not found", Hre())
        {
            StatusCode = HttpStatusCode.NotFound,
            ResponseBody = "not found",
        };

        Assert.False(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void NetworkIoFailure_IsTransient()
    {
        var ex = new AnthropicIOException("socket reset", Hre());

        Assert.True(AnthropicTransient.IsTransient(ex));
    }

    [Fact]
    public void HttpRequestException_IsTransient()
    {
        Assert.True(AnthropicTransient.IsTransient(new HttpRequestException("connection refused")));
    }

    [Fact]
    public void TaskCanceled_Timeout_IsTransient()
    {
        // An HttpClient request timeout surfaces as a TaskCanceledException.
        Assert.True(AnthropicTransient.IsTransient(new TaskCanceledException("timed out")));
    }

    [Fact]
    public void Timeout_IsTransient()
    {
        Assert.True(AnthropicTransient.IsTransient(new TimeoutException("timed out")));
    }

    [Fact]
    public void ArgumentException_IsNotTransient()
    {
        Assert.False(AnthropicTransient.IsTransient(new ArgumentException("bad arg")));
    }

    [Fact]
    public void FormatException_IsNotTransient()
    {
        // Malformed-response parse failures (TriageVerdictParser / SearchResponseParser) — never retried.
        Assert.False(AnthropicTransient.IsTransient(new FormatException("unparseable JSON")));
    }

    [Fact]
    public void InvalidData_IsNotTransient()
    {
        Assert.False(AnthropicTransient.IsTransient(new AnthropicInvalidDataException("garbage")));
    }

    [Fact]
    public void Null_IsNotTransient()
    {
        Assert.False(AnthropicTransient.IsTransient(null));
    }
}
