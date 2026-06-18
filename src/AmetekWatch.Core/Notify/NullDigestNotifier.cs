using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// A no-op <see cref="IDigestNotifier"/> — the default when no sink is configured. Accepts
/// any digest and delivers it nowhere.
/// </summary>
public sealed class NullDigestNotifier : IDigestNotifier
{
    public Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct) =>
        Task.CompletedTask;
}
