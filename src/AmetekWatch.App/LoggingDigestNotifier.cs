using AmetekWatch.Core.Model;
using AmetekWatch.Core.Notify;
using Microsoft.Extensions.Logging;

namespace AmetekWatch.App;

/// <summary>
/// An <see cref="IDigestNotifier"/> decorator that logs one line per completed sweep (the
/// worth-reporting count) and then delegates delivery to an inner notifier. Used by the daemon to
/// satisfy "log each sweep" without modifying the <see cref="SweepHost"/>/<c>SweepRunner</c> core —
/// the host delivers exactly one digest per sweep, so each delivery is one sweep.
/// </summary>
public sealed class LoggingDigestNotifier : IDigestNotifier
{
    private readonly IDigestNotifier _inner;
    private readonly ILogger _logger;

    public LoggingDigestNotifier(IDigestNotifier inner, ILogger logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);
        _logger.LogInformation("Sweep complete: {Count} finding(s) worth reporting.", digest.Count);
        await _inner.NotifyAsync(digest, ct).ConfigureAwait(false);
    }
}
