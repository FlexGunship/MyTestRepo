namespace AmetekWatch.Core.Model;

/// <summary>Pairs a <see cref="Finding"/> with the <see cref="TriageVerdict"/> rendered for it.</summary>
/// <param name="Finding">The finding under judgment.</param>
/// <param name="Verdict">The triage tier's verdict.</param>
public sealed record TriagedFinding(
    Finding Finding,
    TriageVerdict Verdict);
