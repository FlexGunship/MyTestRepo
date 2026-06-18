using AmetekWatch.Core.Model;
using AmetekWatch.Core.Notify;
using AmetekWatch.Core.Pipeline;

namespace AmetekWatch.App;

/// <summary>
/// Hosts the sweep pipeline behind the three Core seams plus a persistence store. A single
/// <see cref="RunOnceAsync"/> drives one <see cref="SweepRunner"/> sweep for the configured
/// subject, delivers the worth-reporting digest through the configured
/// <see cref="IDigestNotifier"/>, and returns that digest; <see cref="RunAsync"/> wraps that in a
/// cancellation-friendly loop for the long-running (service) mode.
/// </summary>
/// <remarks>
/// The host bakes in no clock of its own: discovery timestamps come from the searcher tier (the
/// fakes anchor them deterministically), so a test can pin the digest without controlling wall time.
/// </remarks>
public sealed class SweepHost
{
    private readonly ISearcher _searcher;
    private readonly ITriageDecider _triage;
    private readonly IFindingStore _store;
    private readonly SweepOptions _options;
    private readonly IDigestNotifier _notifier;

    /// <param name="notifier">
    /// Where the worth-reporting digest is delivered after each sweep. Optional — defaults to a
    /// <see cref="NullDigestNotifier"/> (deliver nowhere), preserving the prior no-notifier behaviour.
    /// </param>
    public SweepHost(
        ISearcher searcher,
        ITriageDecider triage,
        IFindingStore store,
        SweepOptions options,
        IDigestNotifier? notifier = null)
    {
        _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
        _triage = triage ?? throw new ArgumentNullException(nameof(triage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _notifier = notifier ?? new NullDigestNotifier();
    }

    /// <summary>
    /// Runs exactly one sweep for the configured subject, persisting every triaged finding to the
    /// store, delivers the worth-reporting digest through the configured
    /// <see cref="IDigestNotifier"/>, and returns that digest (most-recent
    /// <see cref="Finding.DiscoveredAt"/> first — the ordering <see cref="SweepRunner"/> guarantees).
    /// </summary>
    public async Task<IReadOnlyList<TriagedFinding>> RunOnceAsync(CancellationToken ct = default)
    {
        var runner = new SweepRunner(_searcher, _triage, _store);
        var digest = await runner.RunAsync(new SweepQuery(_options.Subject), ct).ConfigureAwait(false);
        await _notifier.NotifyAsync(digest, ct).ConfigureAwait(false);
        return digest;
    }

    /// <summary>
    /// Runs sweeps until cancelled. Always runs at least one sweep; when
    /// <see cref="SweepOptions.RunOnce"/> is <c>true</c> it returns after that single sweep,
    /// otherwise it waits <see cref="SweepOptions.IntervalMinutes"/> between sweeps. Cancellation
    /// surfaces as an <see cref="OperationCanceledException"/> from the wait or the in-flight sweep.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await RunOnceAsync(ct).ConfigureAwait(false);

            if (_options.RunOnce)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), ct).ConfigureAwait(false);
        }
    }
}
