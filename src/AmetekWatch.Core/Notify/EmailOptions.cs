namespace AmetekWatch.Core.Notify;

/// <summary>
/// Configuration for email digest delivery. Bound from app config in a later wiring spec;
/// this type is config-only and carries no secrets — any SMTP password is read from the
/// environment by <see cref="SmtpEmailSender"/>, never stored here or committed.
/// </summary>
/// <param name="Enabled">Whether the email sink is active (a later wiring spec honours this).</param>
/// <param name="SmtpHost">SMTP server host name.</param>
/// <param name="SmtpPort">SMTP server port (e.g. 587 for STARTTLS).</param>
/// <param name="From">The sender address.</param>
/// <param name="To">One or more recipient addresses.</param>
/// <param name="SubjectPrefix">Prefix placed before the friendly subject, e.g. <c>"AMETEK Watch"</c>.</param>
public sealed record EmailOptions(
    bool Enabled,
    string SmtpHost,
    int SmtpPort,
    string From,
    string[] To,
    string SubjectPrefix);
