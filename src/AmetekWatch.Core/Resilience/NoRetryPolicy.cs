namespace AmetekWatch.Core.Resilience;

/// <summary>
/// The default policy: a single attempt with no retry. <see cref="ExecuteAsync{T}"/> simply runs
/// the operation once and lets any exception propagate. Used when callers opt out of retry (and is
/// the <see cref="Pipeline.SweepRunner"/> default, preserving its original behaviour).
/// </summary>
public sealed class NoRetryPolicy : IRetryPolicy
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(op);
        return op(ct);
    }
}
