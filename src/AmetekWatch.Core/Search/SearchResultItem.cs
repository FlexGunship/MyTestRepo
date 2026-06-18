namespace AmetekWatch.Core.Search;

/// <summary>
/// One raw search hit as the real (Sonnet 4.6 + server-side <c>web_search</c>) searcher tier will
/// yield it, before classification or dedupe. This is the searcher's wire shape; the pure
/// <see cref="SearchResultMapper"/> turns it into a domain <see cref="Model.Finding"/>.
/// </summary>
/// <param name="Url">Canonical source URL — becomes the finding's dedupe key.</param>
/// <param name="Title">Headline / page title.</param>
/// <param name="Snippet">Short excerpt or summary of the source.</param>
/// <param name="PublishedAt">When the source was published, if the search result reports it.</param>
/// <param name="SourceDomain">
/// The host the result came from, if the searcher surfaces it separately from <see cref="Url"/>
/// (e.g. <c>"sec.gov"</c>). Used as a classification signal; may be null.
/// </param>
public sealed record SearchResultItem(
    string Url,
    string Title,
    string Snippet,
    DateTimeOffset? PublishedAt,
    string? SourceDomain);
