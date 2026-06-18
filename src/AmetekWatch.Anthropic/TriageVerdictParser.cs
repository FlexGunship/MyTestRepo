using System.Text.Json;
using AmetekWatch.Core.Model;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Pure parser that maps the model's structured-output JSON (the schema built by
/// <see cref="TriageRequestFactory"/>) onto a <see cref="TriageVerdict"/>. Strict by design: any
/// malformed JSON, missing field, or wrong type throws a <see cref="FormatException"/> with the
/// offending payload, rather than silently coercing a half-formed verdict.
/// </summary>
public sealed class TriageVerdictParser
{
    /// <summary>
    /// Parses <paramref name="json"/> into a <see cref="TriageVerdict"/>. Throws
    /// <see cref="ArgumentNullException"/> if null, or <see cref="FormatException"/> if the JSON is
    /// invalid or any of the four verdict fields is missing or the wrong type.
    /// </summary>
    public TriageVerdict Parse(string json)
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
            throw new FormatException($"Triage verdict was not valid JSON: {json}", ex);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Triage verdict JSON was not an object: {json}");
        }

        return new TriageVerdict(
            Important: ReadBool(root, "important", json),
            Relevant: ReadBool(root, "relevant", json),
            WorthReporting: ReadBool(root, "worthReporting", json),
            Rationale: ReadString(root, "rationale", json));
    }

    private static bool ReadBool(JsonElement root, string name, string json)
    {
        if (!root.TryGetProperty(name, out var prop)
            || (prop.ValueKind != JsonValueKind.True && prop.ValueKind != JsonValueKind.False))
        {
            throw new FormatException($"Triage verdict JSON missing boolean field '{name}': {json}");
        }

        return prop.GetBoolean();
    }

    private static string ReadString(JsonElement root, string name, string json)
    {
        if (!root.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"Triage verdict JSON missing string field '{name}': {json}");
        }

        return prop.GetString()!;
    }
}
