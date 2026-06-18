using AmetekWatch.Core.Resilience;

namespace AmetekWatch.Tests;

/// <summary>
/// Tests for <see cref="RetryPolicy"/> and <see cref="NoRetryPolicy"/>. Every oracle is
/// hand-computed against the attempt count and outcome; a <b>no-op delay</b> is injected so the
/// backoff loop runs instantly with no real waiting.
/// </summary>
public class RetryPolicyTests
{
    // Injected delay that does nothing — exercises the backoff path without sleeping.
    private static readonly Func<TimeSpan, CancellationToken, Task> NoDelay =
        (_, _) => Task.CompletedTask;

    private sealed class TransientException : Exception { }

    private sealed class FatalException : Exception { }

    [Fact]
    public async Task RetryPolicy_TransientThenSuccess_ReturnsResult()
    {
        // Throws transient on attempts 1 and 2, succeeds on attempt 3. maxAttempts = 3 (just enough).
        var calls = 0;
        var policy = new RetryPolicy(
            maxAttempts: 3,
            baseDelay: TimeSpan.FromSeconds(1),
            shouldRetry: ex => ex is TransientException,
            delay: NoDelay);

        var result = await policy.ExecuteAsync<int>(_ =>
        {
            calls++;
            if (calls < 3)
            {
                throw new TransientException();
            }

            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(3, calls); // two transient failures + one success
    }

    [Fact]
    public async Task RetryPolicy_GivesUpAfterMaxAttempts_RethrowsLast()
    {
        // Always throws transient; maxAttempts = 3 → tried exactly 3 times, then the last rethrows.
        var calls = 0;
        var policy = new RetryPolicy(
            maxAttempts: 3,
            baseDelay: TimeSpan.FromSeconds(1),
            shouldRetry: ex => ex is TransientException,
            delay: NoDelay);

        await Assert.ThrowsAsync<TransientException>(() =>
            policy.ExecuteAsync<int>(_ =>
            {
                calls++;
                throw new TransientException();
            }, CancellationToken.None));

        Assert.Equal(3, calls); // no 4th attempt
    }

    [Fact]
    public async Task RetryPolicy_NonRetryableException_IsNotRetried()
    {
        // shouldRetry returns false for this exception → exactly one attempt, then propagate.
        var calls = 0;
        var policy = new RetryPolicy(
            maxAttempts: 5,
            baseDelay: TimeSpan.FromSeconds(1),
            shouldRetry: ex => ex is TransientException, // FatalException is NOT retryable
            delay: NoDelay);

        await Assert.ThrowsAsync<FatalException>(() =>
            policy.ExecuteAsync<int>(_ =>
            {
                calls++;
                throw new FatalException();
            }, CancellationToken.None));

        Assert.Equal(1, calls); // not retried despite maxAttempts = 5
    }

    [Fact]
    public async Task NoRetryPolicy_RunsExactlyOnce_AndPropagates()
    {
        var calls = 0;
        var policy = new NoRetryPolicy();

        // Success path runs once.
        var ok = await policy.ExecuteAsync(_ =>
        {
            calls++;
            return Task.FromResult(7);
        }, CancellationToken.None);
        Assert.Equal(7, ok);
        Assert.Equal(1, calls);

        // Failure path is not retried.
        calls = 0;
        await Assert.ThrowsAsync<TransientException>(() =>
            policy.ExecuteAsync<int>(_ =>
            {
                calls++;
                throw new TransientException();
            }, CancellationToken.None));
        Assert.Equal(1, calls);
    }
}
