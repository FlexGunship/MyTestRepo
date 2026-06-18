namespace AmetekWatch.Core.Resilience;

/// <summary>
/// Retries a failing operation with exponential backoff, up to <c>maxAttempts</c> total tries.
/// A retry happens only when the configured <c>shouldRetry</c> predicate accepts the thrown
/// exception (e.g. transient API errors); a non-retryable exception propagates immediately. After
/// the final attempt fails the last exception is rethrown.
/// </summary>
/// <remarks>
/// The inter-attempt wait is <b>injected</b> (<c>Func&lt;TimeSpan, CancellationToken, Task&gt;</c>,
/// defaulting to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>) so tests can pass a no-op
/// delay and exercise the backoff loop without real waiting. Backoff for the wait <i>before</i> the
/// <c>n</c>-th retry (1-based) is <c>baseDelay * 2^(n-1)</c>.
/// </remarks>
public sealed class RetryPolicy : IRetryPolicy
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _baseDelay;
    private readonly Func<Exception, bool> _shouldRetry;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    /// <param name="maxAttempts">Total attempts (including the first). Must be at least 1.</param>
    /// <param name="baseDelay">Base backoff; the wait before retry <c>n</c> is <c>baseDelay * 2^(n-1)</c>.</param>
    /// <param name="shouldRetry">Predicate deciding whether a thrown exception is retryable.</param>
    /// <param name="delay">Injected async wait; defaults to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    public RetryPolicy(
        int maxAttempts,
        TimeSpan baseDelay,
        Func<Exception, bool> shouldRetry,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts), maxAttempts, "maxAttempts must be at least 1.");
        }

        _maxAttempts = maxAttempts;
        _baseDelay = baseDelay;
        _shouldRetry = shouldRetry ?? throw new ArgumentNullException(nameof(shouldRetry));
        _delay = delay ?? ((d, c) => Task.Delay(d, c));
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(op);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await op(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _maxAttempts && _shouldRetry(ex))
            {
                // Wait baseDelay * 2^(attempt-1), then retry. The delay is injected so tests
                // can supply a no-op and the loop runs instantly.
                var backoff = TimeSpan.FromTicks(_baseDelay.Ticks * (1L << (attempt - 1)));
                await _delay(backoff, ct).ConfigureAwait(false);
            }
        }
    }
}
