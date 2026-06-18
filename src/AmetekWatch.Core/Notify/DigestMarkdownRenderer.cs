using System.Globalization;
using System.Text;
using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// Renders a worth-reporting digest as a friendly, human-readable Markdown report:
/// a heading naming the watched subject and the run time, a worth-reporting count, then
/// one section per item with its kind, title, link, and the assessment behind it.
/// <para>
/// The rendered text uses friendly labels only — no internal type, property, or enum names
/// leak into the output. The renderer is pure: no I/O and no ambient clock (the run time is
/// passed in), so the output is fully deterministic and testable.
/// </para>
/// <para>
/// Shared by every digest sink (the file sink and the email sink) so the friendly rendering
/// lives in exactly one place.
/// </para>
/// </summary>
public sealed class DigestMarkdownRenderer
{
    /// <summary>
    /// Builds the friendly Markdown report for <paramref name="digest"/>. An empty digest
    /// renders a clean "nothing to report" notice rather than an empty body.
    /// </summary>
    /// <param name="digest">The worth-reporting digest, already ordered by the caller.</param>
    /// <param name="subject">The watched subject named in the heading, e.g. <c>"AMETEK"</c>.</param>
    /// <param name="runTime">The run time stamped into the heading; converted to UTC.</param>
    public string Render(
        IReadOnlyList<TriagedFinding> digest,
        string subject,
        DateTimeOffset runTime)
    {
        ArgumentNullException.ThrowIfNull(digest);
        ArgumentNullException.ThrowIfNull(subject);

        var when = runTime
            .ToUniversalTime()
            .ToString("dddd, dd MMMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append("# ").Append(subject).AppendLine(" Watch digest");
        sb.AppendLine();
        sb.Append("_Generated ").Append(when).AppendLine("_");
        sb.AppendLine();

        if (digest.Count == 0)
        {
            sb.AppendLine("Nothing to report this run.");
            return sb.ToString();
        }

        var noun = digest.Count == 1 ? "item" : "items";
        sb.Append("**").Append(digest.Count).Append(' ').Append(noun)
            .AppendLine(" worth reporting.**");
        sb.AppendLine();

        foreach (var item in digest)
        {
            var finding = item.Finding;
            sb.Append("## ").Append(FriendlyKind(finding.Category)).Append(": ")
                .AppendLine(finding.Title);
            sb.AppendLine();
            sb.Append("- Link: ").AppendLine(finding.Url);
            sb.Append("- Why it matters: ").AppendLine(item.Verdict.Rationale);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Maps a finding's kind to a reader-facing label (no internal enum names).</summary>
    private static string FriendlyKind(FindingCategory category) => category switch
    {
        FindingCategory.OpinionSocial => "Opinion / Social",
        FindingCategory.FinancialReport => "Financial Report",
        FindingCategory.Other => "Other",
        _ => "Other",
    };
}
