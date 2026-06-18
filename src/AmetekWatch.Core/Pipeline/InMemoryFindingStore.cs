using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Volatile <see cref="IFindingStore"/> for the slice and for tests. Keeps every saved
/// finding in insertion order. A durable store replaces this in a later spec.
/// </summary>
public sealed class InMemoryFindingStore : IFindingStore
{
    private readonly List<TriagedFinding> _items = new();

    public Task SaveAsync(TriagedFinding tf, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tf);
        _items.Add(tf);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TriagedFinding>> GetAllAsync(CancellationToken ct)
    {
        IReadOnlyList<TriagedFinding> snapshot = _items.ToList();
        return Task.FromResult(snapshot);
    }
}
