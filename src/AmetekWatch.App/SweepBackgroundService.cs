using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmetekWatch.App;

/// <summary>
/// Generic-Host <see cref="BackgroundService"/> that drives the periodic sweep loop for the
/// long-lived (daemon) mode. <see cref="ExecuteAsync"/> runs the existing
/// <see cref="SweepHost.RunAsync"/> interval loop, passing the host's <c>stoppingToken</c> straight
/// through so a Ctrl+C / SIGTERM unwinds the loop and the in-flight sweep <b>gracefully</b>.
/// </summary>
/// <remarks>
/// This service adds lifecycle and logging only — it does not change the <see cref="SweepHost"/> or
/// <c>SweepRunner</c> seams. Start/stop are logged here; each completed sweep is logged by the
/// <see cref="LoggingDigestNotifier"/> the host is composed with (one digest delivery per sweep),
/// so "log each sweep" needs no change to the sweep core.
/// </remarks>
public sealed class SweepBackgroundService : BackgroundService
{
    private readonly SweepHost _host;
    private readonly SweepOptions _options;
    private readonly ILogger<SweepBackgroundService> _logger;

    public SweepBackgroundService(
        SweepHost host,
        SweepOptions options,
        ILogger<SweepBackgroundService> logger)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so a synchronous-completing sweep loop (e.g. a zero/short interval over
        // a store whose async calls complete synchronously) cannot block host startup — the Generic
        // Host runs ExecuteAsync inline until its first await. After this, the loop runs in the
        // background and Ctrl+C / SIGTERM unwinds it.
        await Task.Yield();

        _logger.LogInformation(
            "AMETEK Watch sweep daemon starting (subject \"{Subject}\", interval {Interval} min).",
            _options.Subject,
            _options.IntervalMinutes);

        try
        {
            // The existing interval loop. RunOnce is false in daemon mode, so this loops until the
            // stopping token is signalled; the token threads through the wait and the in-flight sweep.
            await _host.RunAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on graceful shutdown — swallow so the host stops cleanly.
        }
        finally
        {
            _logger.LogInformation("AMETEK Watch sweep daemon stopped.");
        }
    }
}
