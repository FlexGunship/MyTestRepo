using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Search;

/// <summary>
/// Builds the set of web-search query strings for one sweep. Pure and deterministic: the same
/// <see cref="SweepQuery"/> always yields the same ordered, de-duplicated list. No I/O, no clock,
/// no network — this is the query-construction half of the searcher tier, built ahead of the
/// SDK-backed <see cref="Pipeline.ISearcher"/> wiring.
/// </summary>
public static class SearchQueryBuilder
{
    // The two focus areas the charter weights, expressed as query suffixes appended to the
    // subject. Order here defines output order: general first, then each focus area.
    //   1. opinion / social sentiment
    //   2. reputable financial reports / earnings
    private const string OpinionSuffix = "opinion OR commentary OR social sentiment";
    private const string FinancialSuffix = "earnings OR financial report OR SEC filing";

    /// <summary>
    /// Produces the ordered, de-duplicated query strings for <paramref name="query"/>: a general
    /// subject query, an opinion/social-sentiment query, and a financial-report/earnings query.
    /// Deterministic; case-sensitive de-duplication preserves first-seen order.
    /// </summary>
    /// <param name="query">The sweep to build queries for; must not be null.</param>
    /// <returns>Between one and three query strings, in fixed order, with no duplicates.</returns>
    public static IReadOnlyList<string> BuildQueries(SweepQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var subject = query.Subject.Trim();

        // Fixed order: general subject query, then one query per focus area.
        var candidates = new[]
        {
            subject,
            $"{subject} {OpinionSuffix}",
            $"{subject} {FinancialSuffix}",
        };

        // De-duplicate while preserving first-seen order (a blank subject would collapse the
        // suffixed forms toward each other; this keeps the list clean and deterministic).
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>(candidates.Length);
        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate))
            {
                ordered.Add(candidate);
            }
        }

        return ordered;
    }
}
