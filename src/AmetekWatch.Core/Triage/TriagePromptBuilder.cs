using System.Globalization;
using System.Text;
using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Triage;

/// <summary>
/// Builds the two halves of a triage request from pure inputs: the system-prompt rubric and a
/// deterministic, labelled rendering of a single <see cref="Finding"/> as the user message. No I/O,
/// no model call — the eventual triage tier (Opus 4.8) consumes what this produces.
///
/// <para>Determinism is a contract: identical inputs always yield byte-identical output, so a
/// finding's prompt can be cached, diffed, and asserted on.</para>
/// </summary>
public static class TriagePromptBuilder
{
    /// <summary>Sentinel rendered when an optional field is absent, so the model sees an explicit
    /// "unknown" rather than a blank line it might misread.</summary>
    private const string Unknown = "(unknown)";

    /// <summary>
    /// Returns the system-prompt rubric the triage tier judges against (see <see cref="TriageRubric"/>).
    /// </summary>
    public static string BuildSystemPrompt() => TriageRubric.SystemPrompt;

    /// <summary>
    /// Renders a single finding as the deterministic, labelled user message. Every field is on its
    /// own labelled line; <see cref="Finding.PublishedAt"/>, being optional, falls back to
    /// <see cref="Unknown"/> when null. Other string fields are emitted verbatim.
    /// </summary>
    /// <param name="finding">The finding to render. Must not be null.</param>
    public static string BuildUserContent(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var publishedAt = finding.PublishedAt is { } at
            ? at.ToString("O", CultureInfo.InvariantCulture)
            : Unknown;

        var sb = new StringBuilder();
        sb.Append("Judge this AMETEK candidate finding against the rubric.\n\n");
        sb.Append("Category: ").Append(finding.Category).Append('\n');
        sb.Append("Title: ").Append(finding.Title).Append('\n');
        sb.Append("Url: ").Append(finding.Url).Append('\n');
        sb.Append("Snippet: ").Append(finding.Snippet).Append('\n');
        sb.Append("PublishedAt: ").Append(publishedAt);
        return sb.ToString();
    }
}
