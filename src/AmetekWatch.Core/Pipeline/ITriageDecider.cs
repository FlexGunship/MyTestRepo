using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// The triage tier (eventually Opus 4.8): renders a <see cref="TriageVerdict"/> for a single finding.
/// </summary>
public interface ITriageDecider
{
    Task<TriageVerdict> JudgeAsync(Finding finding, CancellationToken ct);
}
