using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Persistence seam for triaged findings. The slice ships only an in-memory implementation;
/// a durable (SQLite) store arrives in a later spec behind this same interface.
/// </summary>
public interface IFindingStore
{
    Task SaveAsync(TriagedFinding tf, CancellationToken ct);

    Task<IReadOnlyList<TriagedFinding>> GetAllAsync(CancellationToken ct);
}
