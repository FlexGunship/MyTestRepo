namespace AmetekWatch.Core.Model;

/// <summary>
/// The triage tier's judgment of a single <see cref="Finding"/>. <see cref="WorthReporting"/>
/// is the digest gate: only worth-reporting findings reach the digest, though every triaged
/// finding is persisted.
/// </summary>
/// <param name="Important">The finding is materially significant.</param>
/// <param name="Relevant">The finding actually concerns the swept subject.</param>
/// <param name="WorthReporting">The finding should appear in the digest.</param>
/// <param name="Rationale">Human-readable explanation for the verdict.</param>
public sealed record TriageVerdict(
    bool Important,
    bool Relevant,
    bool WorthReporting,
    string Rationale);
