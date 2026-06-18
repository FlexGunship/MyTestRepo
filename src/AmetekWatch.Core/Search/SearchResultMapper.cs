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
///   <item><see cref="FindingCategory.FinancialReport"/> — the source <b>domain</b> is an
///   institutional/regulator/IR host (SEC/EDGAR, investor-relations). A financial source domain
///   is authoritative and wins regardless of the title.</item>
///   <item><see cref="FindingCategory.OpinionSocial"/> — the source <b>domain</b> is a known
///   social/blogging platform, OR the <b>title</b> signals opinion/commentary (op-ed, opinion,
///   blog). A social source wins over a financial-title signal: this tool gives special weight to
///   personal/social opinion, so e.g. a LinkedIn post titled "AMETEK earnings reaction" is social
///   commentary <i>about</i> earnings, not an institutional financial report.</item>
///   <item><see cref="FindingCategory.FinancialReport"/> — the <b>title</b> names a financial
///   report (earnings, 10-Q, 10-K, annual report) from a non-social, non-IR source (e.g. a news
///   article reporting results).</item>
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

        // 1. FinancialReport — an institutional/regulator/IR source DOMAIN is authoritative and
        //    wins regardless of the title (e.g. sec.gov, ir.<company>.com).
        if (ContainsAny(domain, FinancialDomainSignals))
        {
            return FindingCategory.FinancialReport;
        }

        // 2. OpinionSocial — a known social/blogging source DOMAIN, or an opinion/commentary
        //    TITLE. A social source wins over a financial-title signal: special weight goes to
        //    personal/social opinion, so a social post titled "AMETEK earnings reaction" is
        //    commentary about earnings, not an institutional financial report.
        if (ContainsAny(domain, SocialDomainSignals) || ContainsAny(title, OpinionTitleSignals))
        {
            return FindingCategory.OpinionSocial;
        }

        // 3. FinancialReport — a financial-report TITLE (earnings/10-Q/10-K/annual report) from a
        //    non-social, non-IR source (e.g. a news article reporting quarterly results).
        if (ContainsAny(title, FinancialTitleSignals))
        {
            return FindingCategory.FinancialReport;
        }

        // 4. Other — no recognised signal.
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
