using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// The searcher tier (eventually Sonnet 4.6 + web search): turns a <see cref="SweepQuery"/>
/// into a flat list of candidate findings. May return duplicate URLs — dedupe is the
/// orchestrator's job, not the searcher's.
/// </summary>
public interface ISearcher
{
    Task<IReadOnlyList<Finding>> SweepAsync(SweepQuery query, CancellationToken ct);
}
