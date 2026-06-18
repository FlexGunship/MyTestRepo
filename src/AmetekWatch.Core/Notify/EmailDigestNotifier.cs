using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// Delivers the worth-reporting digest by email. Renders the friendly Markdown body with the
/// shared <see cref="DigestMarkdownRenderer"/> (the same friendly-names-only output the file
/// sink uses) and hands subject + body to an <see cref="IEmailSender"/> transport.
/// <para>
/// The subject is a friendly summary, e.g. <c>"AMETEK Watch — 2 findings worth reporting"</c>
/// (the <see cref="EmailOptions.SubjectPrefix"/> followed by the count). An <b>empty</b> digest
/// still sends a clean "nothing to report" email — the renderer produces the notice body and the
/// subject reads <c>"… — nothing to report"</c>; callers who prefer to skip empty runs can guard
/// the call upstream.
/// </para>
/// <para>
/// The run timestamp is injected (a provider), never read from the ambient clock, so the body
/// is fully deterministic and testable. No live network I/O lives here — that is confined to the
/// injected sender.
/// </para>
/// </summary>
public sealed class EmailDigestNotifier : IDigestNotifier
{
    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;
    private readonly DigestMarkdownRenderer _renderer;
    private readonly string _subject;
    private readonly Func<DateTimeOffset> _timestampProvider;

    /// <param name="sender">Transport that performs the send (a fake in tests, SMTP in production).</param>
    /// <param name="options">Email configuration, including the subject prefix.</param>
    /// <param name="renderer">The shared friendly-Markdown renderer.</param>
    /// <param name="subject">The watched subject named in the heading, e.g. <c>"AMETEK"</c>.</param>
    /// <param name="timestampProvider">
    /// Supplies the run time stamped into the body. Injected so no ambient clock is read.
    /// </param>
    public EmailDigestNotifier(
        IEmailSender sender,
        EmailOptions options,
        DigestMarkdownRenderer renderer,
        string subject,
        Func<DateTimeOffset> timestampProvider)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        _timestampProvider = timestampProvider
            ?? throw new ArgumentNullException(nameof(timestampProvider));
    }

    public Task NotifyAsync(IReadOnlyList<TriagedFinding> digest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(digest);
        var body = _renderer.Render(digest, _subject, _timestampProvider());
        var subject = BuildSubject(digest.Count);
        return _sender.SendAsync(subject, body, ct);
    }

    /// <summary>Friendly subject line — prefix plus a plain-English count (no internal names).</summary>
    private string BuildSubject(int count)
    {
        if (count == 0)
        {
            return $"{_options.SubjectPrefix} — nothing to report";
        }

        var noun = count == 1 ? "finding" : "findings";
        return $"{_options.SubjectPrefix} — {count} {noun} worth reporting";
    }
}
