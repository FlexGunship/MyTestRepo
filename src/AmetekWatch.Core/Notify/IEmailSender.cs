namespace AmetekWatch.Core.Notify;

/// <summary>
/// Transport seam for sending an email. Splits the pure, testable notifier logic (subject +
/// body composition) from the one untestable line — the live SMTP send — mirroring the
/// Anthropic <c>IMessagesClient</c> pattern. Tests use a fake implementation that captures
/// the call; production uses <see cref="SmtpEmailSender"/>.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends a single email with the given <paramref name="subject"/> and <paramref name="body"/>.</summary>
    Task SendAsync(string subject, string body, CancellationToken ct);
}
