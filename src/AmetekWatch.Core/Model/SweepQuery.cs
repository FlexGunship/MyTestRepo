namespace AmetekWatch.Core.Model;

/// <summary>
/// Describes one sweep: what subject to search for, and an optional cap on results.
/// </summary>
/// <param name="Subject">The subject to sweep for, e.g. <c>"AMETEK"</c>.</param>
/// <param name="MaxResults">Optional cap on the number of results the searcher returns.</param>
public sealed record SweepQuery(
    string Subject,
    int? MaxResults = null);
