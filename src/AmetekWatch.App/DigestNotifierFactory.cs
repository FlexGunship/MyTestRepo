using AmetekWatch.Core.Notify;

namespace AmetekWatch.App;

/// <summary>
/// Resolves the <see cref="IDigestNotifier"/> the host delivers through, selected by
/// <see cref="NotifyOptions.Sink"/>:
/// <list type="bullet">
///   <item><c>"File"</c> → <see cref="FileDigestNotifier"/> writing <see cref="NotifyOptions.DigestPath"/>.</item>
///   <item><c>"Email"</c> → <see cref="EmailDigestNotifier"/> over a live <see cref="SmtpEmailSender"/>.</item>
///   <item><c>"None"</c>, an unrecognized sink, a File sink with no path, or an incomplete/disabled
///         email config → <see cref="NullDigestNotifier"/> (with a logged warning where a delivery
///         was requested but cannot be honoured).</item>
/// </list>
/// </summary>
/// <remarks>
/// Construction only — like <c>PipelineFactory</c>, this builds objects but invokes nothing, so the
/// Email path constructs (but never calls) <see cref="SmtpEmailSender"/>: no SMTP send and no network
/// happen here, and the tests assert the resolved runtime type directly. Warnings are emitted through
/// an injected callback (default no-op) rather than an ambient logger, keeping the helper pure and
/// testable; <c>Program</c> passes <c>Console.WriteLine</c>.
/// </remarks>
public static class DigestNotifierFactory
{
    /// <summary>
    /// Builds the digest sink from <paramref name="notify"/>. <paramref name="subject"/> names the
    /// watched subject in the rendered heading; <paramref name="clock"/> stamps the run time (injected
    /// so no ambient clock is read). <paramref name="warn"/> receives a human-readable message when a
    /// requested delivery falls back to the no-op sink.
    /// </summary>
    public static IDigestNotifier Create(
        NotifyOptions notify,
        string subject,
        Func<DateTimeOffset> clock,
        Action<string>? warn = null)
    {
        ArgumentNullException.ThrowIfNull(notify);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(clock);
        var log = warn ?? (_ => { });

        var sink = (notify.Sink ?? "File").Trim();

        if (string.Equals(sink, "None", StringComparison.OrdinalIgnoreCase))
        {
            return new NullDigestNotifier();
        }

        if (string.Equals(sink, "Email", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsEmailUsable(notify.Email))
            {
                log("WARNING: Notify:Sink is \"Email\" but the Email config is missing, disabled, or "
                    + "incomplete — no digest will be delivered.");
                return new NullDigestNotifier();
            }

            return new EmailDigestNotifier(
                new SmtpEmailSender(notify.Email!),
                notify.Email!,
                new DigestMarkdownRenderer(),
                subject,
                clock);
        }

        if (string.Equals(sink, "File", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(notify.DigestPath))
            {
                log("WARNING: Notify:Sink is \"File\" but Notify:DigestPath is empty — no digest file "
                    + "will be written.");
                return new NullDigestNotifier();
            }

            return new FileDigestNotifier(notify.DigestPath, subject, clock);
        }

        log($"WARNING: Notify:Sink \"{sink}\" is not recognized (expected File/Email/None) — no digest "
            + "will be delivered.");
        return new NullDigestNotifier();
    }

    /// <summary>
    /// A usable email config is enabled and has a host, a sender, at least one non-empty recipient,
    /// and a subject prefix. Anything less falls back to the no-op sink.
    /// </summary>
    private static bool IsEmailUsable(EmailOptions? email)
    {
        if (email is null || !email.Enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(email.SmtpHost)
            || string.IsNullOrWhiteSpace(email.From)
            || string.IsNullOrWhiteSpace(email.SubjectPrefix))
        {
            return false;
        }

        return email.To is { Length: > 0 }
            && email.To.All(to => !string.IsNullOrWhiteSpace(to));
    }
}
