using System.Globalization;
using System.Text;
using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// Writes the digest to a single Markdown file as a friendly, human-readable report:
/// a heading naming the watched subject and the run time, a worth-reporting count, then
/// one section per item with its kind, title, link, and the assessment behind it.
/// <para>
/// The rendered text uses friendly labels only — no internal type or property names leak
/// into the file. The file is overwritten on every run (the latest digest replaces the last).
/// </para>
/// <para>
/// The run timestamp is injected (a provider), never read from the ambient clock, so the
/// output is fully deterministic and testable.
/// </para>
/// </summary>
public sealed class FileDigestNotifier : IDigestNotifier
{
    private readonly string _outputPath;
    private readonly string _subject;
    private readonly Func<DateTimeOffset> _timestampProvider;

    /// <param name="outputPath">File to write (and overwrite) the rendered digest to.</param>
    /// <param name="subject">The watched subject named in the heading, e.g. <c>"AMETEK"</c>.</param>
    /// <param name="timestampProvider">
    /// Supplies the run time stamped into the heading. Injected so no ambient clock is read.
    /// </param>
    public FileDigestNotifier(
        string outputPath,
        string subject,
        Func<DateTimeOffset> timestampProvider)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _timestampProvider = timestampProvider
            ?? throw new ArgumentNullException(nameof(timestampProvider));
    }

    public Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);
        var markdown = Render(digest, _subject, _timestampProvider());
        return File.WriteAllTextAsync(_outputPath, markdown, ct);
    }

    /// <summary>Builds the friendly Markdown report. Pure — no I/O, no clock.</summary>
    internal static string Render(
        IReadOnlyList<TriagedFinding> digest,
        string subject,
        DateTimeOffset runTime)
    {
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
