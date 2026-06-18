namespace AmetekWatch.Core.Model;

/// <summary>
/// A single candidate result surfaced by the searcher tier. <see cref="Url"/> is the
/// dedupe identity: two findings with the same URL are the same finding.
/// </summary>
/// <param name="Url">Canonical source URL — the dedupe key.</param>
/// <param name="Title">Headline / page title.</param>
/// <param name="Snippet">Short excerpt or summary of the source.</param>
/// <param name="PublishedAt">When the source was published, if known.</param>
/// <param name="Category">Coarse classification assigned by the searcher.</param>
/// <param name="DiscoveredAt">When this sweep discovered the finding — the digest sort key.</param>
public sealed record Finding(
    string Url,
    string Title,
    string Snippet,
    DateTimeOffset? PublishedAt,
    FindingCategory Category,
    DateTimeOffset DiscoveredAt);
