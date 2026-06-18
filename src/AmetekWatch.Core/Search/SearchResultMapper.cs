using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Search;

/// <summary>
/// Maps a raw <see cref="SearchResultItem"/> into a domain <see cref="Finding"/>, assigning a
/// coarse <see cref="FindingCategory"/> from a small, documented heuristic. Pure and deterministic:
/// no I/O, no clock — the caller injects <c>discoveredAt</c> so the mapping is fully testable.
/// </summary>
/// <remarks>
/// <para>Category heuristic (checked in this order; first match wins):</para>
/// <list type="number">
///   <item><see cref="FindingCategory.FinancialReport"/> — the source domain looks like a
///   regulator/filing host (SEC/EDGAR) or an investor-relations host, OR the title names a
///   financial report (earnings, 10-Q, 10-K, annual report).</item>
///   <item><see cref="FindingCategory.OpinionSocial"/> — the title signals opinion/commentary
///   (op-ed, opinion, blog) OR the source domain is a known social/blogging platform.</item>
///   <item><see cref="FindingCategory.Other"/> — anything else.</item>
/// </list>
/// The signal lists below are explicit constants so tests can pin the boundaries. Matching is
/// case-insensitive (invariant); domain checks look at <see cref="SearchResultItem.SourceDomain"/>
/// when present and otherwise at the <see cref="SearchResultItem.Url"/>.
/// </remarks>
public static class SearchResultMapper
{
    // --- FinancialReport signals -------------------------------------------------------------
    // Domains that signal an institutional/regulatory filing or investor-relations source.
    private static readonly string[] FinancialDomainSignals =
    {
        "sec.gov",   // U.S. SEC
        "edgar",     // EDGAR filing system (e.g. efts.sec.gov, edgar-online)
        "investor.", // investor.<company>.com
        "ir.",       // ir.<company>.com (investor relations)
    };

    // Title phrases that name a financial report or filing.
    private static readonly string[] FinancialTitleSignals =
    {
        "earnings",
        "10-q",
        "10-k",
        "annual report",
    };

    // --- OpinionSocial signals ---------------------------------------------------------------
    // Title phrases that signal opinion / commentary.
    private static readonly string[] OpinionTitleSignals =
    {
        "op-ed",
        "opinion",
        "blog",
    };

    // Known social / blogging platform domains.
    private static readonly string[] SocialDomainSignals =
    {
        "twitter.com",
        "x.com",
        "reddit.com",
        "facebook.com",
        "linkedin.com",
        "medium.com",
    };

    /// <summary>
    /// Builds a <see cref="Finding"/> from <paramref name="item"/>, classifying its category by the
    /// heuristic documented on this type and stamping it with the injected
    /// <paramref name="discoveredAt"/>.
    /// </summary>
    /// <param name="item">The raw search hit; must not be null.</param>
    /// <param name="discoveredAt">When this sweep discovered the item (injected — never read from a clock).</param>
    public static Finding ToFinding(SearchResultItem item, DateTimeOffset discoveredAt)
    {
        ArgumentNullException.ThrowIfNull(item);

        var category = Classify(item);

        return new Finding(
            Url: item.Url,
            Title: item.Title,
            Snippet: item.Snippet,
            PublishedAt: item.PublishedAt,
            Category: category,
            DiscoveredAt: discoveredAt);
    }

    private static FindingCategory Classify(SearchResultItem item)
    {
        // Prefer the explicit SourceDomain signal; fall back to the URL when it is absent.
        var domain = (item.SourceDomain ?? item.Url).ToLowerInvariant();
        var title = (item.Title ?? string.Empty).ToLowerInvariant();

        // 1. FinancialReport — institutional/filing domain or a financial-report title.
        if (ContainsAny(domain, FinancialDomainSignals) || ContainsAny(title, FinancialTitleSignals))
        {
            return FindingCategory.FinancialReport;
        }

        // 2. OpinionSocial — opinion/commentary title or a known social/blogging domain.
        if (ContainsAny(title, OpinionTitleSignals) || ContainsAny(domain, SocialDomainSignals))
        {
            return FindingCategory.OpinionSocial;
        }

        // 3. Other — no recognised signal.
        return FindingCategory.Other;
    }

    private static bool ContainsAny(string haystack, string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
