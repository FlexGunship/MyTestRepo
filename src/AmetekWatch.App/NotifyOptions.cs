using AmetekWatch.Core.Notify;

namespace AmetekWatch.App;

/// <summary>
/// Configuration for digest delivery, bound from the <c>Notify</c> section of
/// <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <see cref="Sink"/> selects the delivery channel: <c>"File"</c> (default) writes the
/// worth-reporting digest to <see cref="DigestPath"/> via <c>FileDigestNotifier</c>; <c>"Email"</c>
/// sends it via <c>EmailDigestNotifier</c> using the <see cref="Email"/> settings; <c>"None"</c>
/// delivers nowhere. An incomplete/disabled <see cref="Email"/> under <c>"Email"</c> falls back to a
/// no-op sink with a logged warning (see <see cref="DigestNotifierFactory"/>).
/// </remarks>
public sealed record NotifyOptions
{
    /// <summary>
    /// Digest delivery channel: <c>"File"</c> (default), <c>"Email"</c>, or <c>"None"</c>.
    /// Matched case-insensitively; an unrecognized value falls back to the no-op sink.
    /// </summary>
    public string Sink { get; init; } = "File";

    /// <summary>
    /// File to write the rendered digest to when <see cref="Sink"/> is <c>"File"</c>. Optional —
    /// empty/absent under the File sink means "do not write a digest file" (a no-op sink is used).
    /// </summary>
    public string? DigestPath { get; init; }

    /// <summary>
    /// Email delivery settings, bound from the <c>Notify:Email</c> subsection and used when
    /// <see cref="Sink"/> is <c>"Email"</c>. May be absent/incomplete; the selection helper
    /// validates it and falls back to a no-op sink if it is not usable.
    /// </summary>
    public EmailOptions? Email { get; init; }
}
