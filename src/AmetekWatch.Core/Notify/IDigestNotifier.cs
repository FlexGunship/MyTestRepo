using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// Delivery seam for a finished digest. A sweep produces the worth-reporting digest; a
/// notifier delivers it somewhere (a file today; email or other sinks are later drop-ins
/// behind this same interface). Implementations decide where and how to render.
/// </summary>
public interface IDigestNotifier
{
    /// <summary>
    /// Delivers <paramref name="digest"/> (the worth-reporting subset of a sweep, already
    /// ordered by the caller). Implementations may render an empty digest as a clean
    /// "nothing to report" notice rather than failing.
    /// </summary>
    Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct);
}
