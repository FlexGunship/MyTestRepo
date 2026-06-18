using AmetekWatch.App;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Notify;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace AmetekWatch.Tests;

/// <summary>
/// Behavioural tests for <see cref="SweepBackgroundService"/> — the daemon wrapper around the
/// existing <see cref="SweepHost.RunAsync"/> interval loop. Driven over the deterministic Core fakes
/// and a real temp-file SQLite store, with a <b>zero-minute</b> interval so the loop iterates as fast
/// as possible: the test waits (bounded) for ≥2 sweeps, then cancels and asserts the service stops
/// promptly. No real time delays — the interval is zero and every wait is bounded by a timeout.
///
/// Sweep count is observed through a counting <see cref="IDigestNotifier"/> (the host delivers one
/// digest per sweep). Persisted findings are the fakes' 4 unique URLs (deduped, upserted) regardless
/// of how many sweeps ran.
/// </summary>
public sealed class SweepBackgroundServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"ametek-watch-bgtest-{Guid.NewGuid():N}.db");

    // A digest notifier that counts deliveries (= sweeps) and signals once a target count is reached.
    private sealed class CountingNotifier : IDigestNotifier
    {
        private readonly int _target;
        private readonly TaskCompletionSource _reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;

        public CountingNotifier(int target) => _target = target;

        public int Count => Volatile.Read(ref _count);
        public Task ReachedTarget => _reached.Task;

        public Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _count) >= _target)
            {
                _reached.TrySetResult();
            }

            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Daemon_RunsAtLeastTwoSweeps_ThenStopsPromptly_OnCancel()
    {
        var store = new SqliteFindingStore(_dbPath);
        var notifier = new CountingNotifier(target: 2);
        var options = new SweepOptions { Subject = "AMETEK", IntervalMinutes = 0, RunOnce = false };
        var host = new SweepHost(new FakeSearcher(), new FakeTriageDecider(), store, options, notifier);

        var service = new SweepBackgroundService(host, options, NullLogger<SweepBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);

        // Bounded wait for ≥2 sweeps — never an unbounded hang even if the loop misbehaves.
        var reached = await Task.WhenAny(notifier.ReachedTarget, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(ReferenceEquals(reached, notifier.ReachedTarget),
            "expected at least two sweeps within the timeout");

        // Cancel and assert the service stops PROMPTLY (well within the bounded window).
        var stop = service.StopAsync(CancellationToken.None);
        var stopped = await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(ReferenceEquals(stopped, stop), "expected the daemon to stop promptly on cancel");
        await stop; // observe any exception

        // Sweeps actually ran...
        Assert.True(notifier.Count >= 2);
        // ...and persisted the fakes' 4 unique findings (deduped by URL; upserted across sweeps).
        var persisted = await store.GetAllAsync(CancellationToken.None);
        Assert.Equal(4, persisted.Count);
    }

    [Fact]
    public async Task RunOnceMode_StillRunsExactlyOneSweep_AndReturns()
    {
        // RunOnce=true: SweepHost.RunAsync runs one sweep and returns (the CLI default path).
        var store = new SqliteFindingStore(_dbPath);
        var notifier = new CountingNotifier(target: 1);
        var options = new SweepOptions { Subject = "AMETEK", IntervalMinutes = 1440, RunOnce = true };
        var host = new SweepHost(new FakeSearcher(), new FakeTriageDecider(), store, options, notifier);

        await host.RunAsync(CancellationToken.None); // returns after one sweep — no hang despite the long interval

        Assert.Equal(1, notifier.Count);
        var persisted = await store.GetAllAsync(CancellationToken.None);
        Assert.Equal(4, persisted.Count);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
