namespace AmetekWatch.Core.Resilience;

/// <summary>
/// A retry strategy for a single asynchronous operation. Implementations decide how many times
/// (and whether) to re-run <paramref name="op"/> when it throws. The result of the last
/// successful attempt is returned; if every attempt fails the final exception propagates.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Runs <paramref name="op"/> under this policy and returns its result, retrying per the
    /// policy when it throws. Rethrows the last exception once the policy gives up.
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct);
}
