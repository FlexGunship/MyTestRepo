using System.Text.Json;
using AmetekWatch.Core.Search;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Pure parser that maps the model's structured-output JSON (the array schema built by
/// <see cref="SearchRequestFactory"/>) onto a list of <see cref="SearchResultItem"/> — the 013 wire
/// shape the <see cref="SearchResultMapper"/> then turns into <c>Finding</c>s. Tolerant of an empty
/// array; strict otherwise — malformed JSON, a non-array root, a non-object element, or a missing /
/// wrong-typed required field throws a <see cref="FormatException"/> carrying the offending payload,
/// rather than silently dropping or coercing a half-formed hit.
/// </summary>
public sealed class SearchResponseParser
{
    /// <summary>
    /// Parses <paramref name="json"/> into the ordered list of search hits. Throws
    /// <see cref="ArgumentNullException"/> if null, or <see cref="FormatException"/> if the JSON is
    /// invalid, is not an array, or any element is missing a required field or has the wrong type.
    /// An empty array yields an empty list.
    /// </summary>
    public IReadOnlyList<SearchResultItem> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Search response was not valid JSON: {json}", ex);
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException($"Search response JSON was not an array: {json}");
        }

        var items = new List<SearchResultItem>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException($"Search response array element was not an object: {json}");
            }

            items.Add(new SearchResultItem(
                Url: ReadString(element, "url", json),
                Title: ReadString(element, "title", json),
                Snippet: ReadString(element, "snippet", json),
                PublishedAt: ReadNullableDate(element, "publishedAt", json),
                SourceDomain: ReadNullableString(element, "sourceDomain", json)));
        }

        return items;
    }

    private static string ReadString(JsonElement element, string name, string json)
    {
        if (!element.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"Search hit JSON missing string field '{name}': {json}");
        }

        return prop.GetString()!;
    }

    /// <summary>Reads an optional string: an absent property or a JSON null both map to <c>null</c>.</summary>
    private static string? ReadNullableString(JsonElement element, string name, string json)
    {
        if (!element.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"Search hit JSON field '{name}' was not a string or null: {json}");
        }

        return prop.GetString();
    }

    /// <summary>
    /// Reads an optional ISO-8601 timestamp: an absent property or JSON null map to <c>null</c>; a
    /// present string must parse as a <see cref="DateTimeOffset"/>.
    /// </summary>
    private static DateTimeOffset? ReadNullableDate(JsonElement element, string name, string json)
    {
        if (!element.TryGetProperty(name, out var prop) || prop.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (prop.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(
                prop.GetString(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            throw new FormatException(
                $"Search hit JSON field '{name}' was not an ISO-8601 timestamp or null: {json}");
        }

        return parsed;
    }
}
