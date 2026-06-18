using AmetekWatch.Core.Model;
using AmetekWatch.Core.Resilience;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Orchestrates one sweep over the three pipeline seams:
/// search → dedupe by <see cref="Finding.Url"/> (first occurrence wins) → triage each
/// survivor → persist <b>every</b> triaged finding → return the digest (the worth-reporting
/// subset, most-recent <see cref="Finding.DiscoveredAt"/> first).
/// </summary>
/// <remarks>
/// Resilience (spec 034): the searcher call runs under an injectable <see cref="IRetryPolicy"/>
/// (default <see cref="NoRetryPolicy"/>, so a searcher failure propagates as before — a sweep can't
/// proceed without results). Each finding's triage is isolated in a try/catch: if the decider throws,
/// the optional <c>onTriageError</c> callback is invoked and that finding is skipped (not persisted,
/// not digested) rather than aborting the whole sweep. Both knobs default to the original behaviour,
/// so existing 3-arg construction is unchanged.
/// <para>
/// "Only new" digest (spec 038): when <c>digestOnlyNew</c> is true, the returned digest reports only
/// worth-reporting findings whose <see cref="Finding.Url"/> was <b>not already in the store</b> before
/// this sweep (snapshotted via <see cref="IFindingStore.GetAllAsync"/> at the start of the run). This
/// keeps a periodic daemon from re-notifying findings it has already reported. Persistence is unchanged
/// — every triaged finding is still upserted. The default (<c>false</c>) preserves current behaviour
/// (the digest is the full worth-reporting subset of this sweep).
/// </para>
/// </remarks>
public sealed class SweepRunner
{
    private readonly ISearcher _searcher;
    private readonly ITriageDecider _triage;
    private readonly IFindingStore _store;
    private readonly IRetryPolicy _retryPolicy;
    private readonly Action<Finding, Exception> _onTriageError;
    private readonly bool _digestOnlyNew;

    public SweepRunner(
        ISearcher searcher,
        ITriageDecider triage,
        IFindingStore store,
        IRetryPolicy? retryPolicy = null,
        Action<Finding, Exception>? onTriageError = null,
        bool digestOnlyNew = false)
    {
        _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
        _triage = triage ?? throw new ArgumentNullException(nameof(triage));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _retryPolicy = retryPolicy ?? new NoRetryPolicy();
        _onTriageError = onTriageError ?? ((_, _) => { });
        _digestOnlyNew = digestOnlyNew;
    }

    /// <summary>
    /// Runs one sweep. Persistence keeps every successfully triaged finding; the returned digest is
    /// the worth-reporting subset, ordered most-recent <see cref="Finding.DiscoveredAt"/> first.
    /// </summary>
    public async Task<IReadOnlyList<TriagedFinding>> RunAsync(
        SweepQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Snapshot the URLs already in the store BEFORE this sweep persists anything. A finding is
        // "new" iff its URL is not in this set. Only consulted when _digestOnlyNew is true, but we
        // take it up front (before any upsert) so the snapshot reflects prior runs, not this one.
        var knownUrls = new HashSet<string>(StringComparer.Ordinal);
        if (_digestOnlyNew)
        {
            var existing = await _store.GetAllAsync(ct).ConfigureAwait(false);
            foreach (var tf in existing)
            {
                knownUrls.Add(tf.Finding.Url);
            }
        }

        // Search under the retry policy. If it still fails after the policy gives up, propagate —
        // a sweep can't proceed without results, and that's the caller's concern.
        var found = await _retryPolicy
            .ExecuteAsync(token => _searcher.SweepAsync(query, token), ct)
            .ConfigureAwait(false);

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

        // Triage and persist every survivor, isolating each finding: a decider failure on one
        // finding skips only that finding (callback + continue), never aborts the sweep.
        var triaged = new List<TriagedFinding>(unique.Count);
        foreach (var finding in unique)
        {
            TriageVerdict verdict;
            try
            {
                verdict = await _retryPolicy
                    .ExecuteAsync(token => _triage.JudgeAsync(finding, token), ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onTriageError(finding, ex);
                continue;
            }

            var tf = new TriagedFinding(finding, verdict);
            await _store.SaveAsync(tf, ct).ConfigureAwait(false);
            triaged.Add(tf);
        }

        // Digest = worth-reporting only, most-recent DiscoveredAt first. When digestOnlyNew is set,
        // also require the URL to be absent from the pre-sweep snapshot (don't re-report known findings).
        return triaged
            .Where(t => t.Verdict.WorthReporting)
            .Where(t => !_digestOnlyNew || !knownUrls.Contains(t.Finding.Url))
            .OrderByDescending(t => t.Finding.DiscoveredAt)
            .ToList();
    }
}
