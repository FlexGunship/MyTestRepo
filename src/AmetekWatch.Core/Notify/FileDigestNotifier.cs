using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// Writes the digest to a single Markdown file as a friendly, human-readable report:
/// a heading naming the watched subject and the run time, a worth-reporting count, then
/// one section per item with its kind, title, link, and the assessment behind it.
/// <para>
/// The friendly Markdown is produced by the shared <see cref="DigestMarkdownRenderer"/> (so
/// the rendering lives in one place, reused by the email sink) — friendly labels only, no
/// internal type or property names leak. The file is overwritten on every run (the latest
/// digest replaces the last).
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
    private readonly DigestMarkdownRenderer _renderer;

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
        _renderer = new DigestMarkdownRenderer();
    }

    public Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);
        var markdown = _renderer.Render(digest, _subject, _timestampProvider());
        return File.WriteAllTextAsync(_outputPath, markdown, ct);
    }
}
