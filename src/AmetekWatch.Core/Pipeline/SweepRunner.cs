using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Orchestrates one sweep over the three pipeline seams:
/// search → dedupe by <see cref="Finding.Url"/> (first occurrence wins) → triage each
/// survivor → persist <b>every</b> triaged finding → return the digest (the worth-reporting
/// subset, most-recent <see cref="Finding.DiscoveredAt"/> first).
/// </summary>
public sealed class SweepRunner
{
    private readonly ISearcher _searcher;
    private readonly ITriageDecider _triage;
    private readonly IFindingStore _store;

    public SweepRunner(ISearcher searcher, ITriageDecider triage, IFindingStore store)
    {
        _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
        _triage = triage ?? throw new ArgumentNullException(nameof(triage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Runs one sweep. Persistence keeps every triaged finding; the returned digest is the
    /// worth-reporting subset, ordered most-recent <see cref="Finding.DiscoveredAt"/> first.
    /// </summary>
    public async Task<IReadOnlyList<TriagedFinding>> RunAsync(
        SweepQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var found = await _searcher.SweepAsync(query, ct).ConfigureAwait(false);

        // Dedupe by URL, first occurrence wins, preserving discovery order.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<Finding>(found.Count);
        foreach (var f in found)
        {
            if (seen.Add(f.Url))
            {
                unique.Add(f);
            }
        }

        // Triage and persist every survivor.
        var triaged = new List<TriagedFinding>(unique.Count);
        foreach (var finding in unique)
        {
            var verdict = await _triage.JudgeAsync(finding, ct).ConfigureAwait(false);
            var tf = new TriagedFinding(finding, verdict);
            await _store.SaveAsync(tf, ct).ConfigureAwait(false);
            triaged.Add(tf);
        }

        // Digest = worth-reporting only, most-recent DiscoveredAt first.
        return triaged
            .Where(t => t.Verdict.WorthReporting)
            .OrderByDescending(t => t.Finding.DiscoveredAt)
            .ToList();
    }
}
