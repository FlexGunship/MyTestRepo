using System.Net;
using System.Net.Mail;

namespace AmetekWatch.Core.Notify;

/// <summary>
/// The live <see cref="IEmailSender"/>: sends a Markdown digest body over SMTP using the BCL
/// <see cref="SmtpClient"/> (no NuGet), configured from <see cref="EmailOptions"/>.
/// <para>
/// This is the <b>only</b> code in this seam not unit-tested — it performs real network I/O,
/// so it is deferred and exercised only once SMTP credentials exist (mirroring the live
/// Anthropic message client). Any SMTP password is read from the environment variable named by
/// <see cref="PasswordEnvVar"/>; it is <b>never</b> hardcoded or committed. With no password in
/// the environment the client sends without explicit credentials (e.g. an open relay on a
/// trusted network).
/// </para>
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    /// <summary>Environment variable holding the SMTP password, if authentication is required.</summary>
    public const string PasswordEnvVar = "AMETEK_WATCH_SMTP_PASSWORD";

    private readonly EmailOptions _options;

    public SmtpEmailSender(EmailOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task SendAsync(string subject, string body, CancellationToken ct)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = subject,
            Body = body,
        };
        foreach (var recipient in _options.To)
        {
            message.To.Add(recipient);
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
        };

        // Password from the environment only — never hardcoded or committed.
        var password = Environment.GetEnvironmentVariable(PasswordEnvVar);
        if (!string.IsNullOrEmpty(password))
        {
            client.Credentials = new NetworkCredential(_options.From, password);
        }

        await client.SendMailAsync(message, ct);
    }
}
