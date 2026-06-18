namespace AmetekWatch.App;

/// <summary>
/// Configuration for digest delivery, bound from the <c>Notify</c> section of
/// <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// When <see cref="DigestPath"/> is set the host writes the worth-reporting digest to that file
/// (via <c>FileDigestNotifier</c>); when it is empty/absent no digest file is written
/// (a <c>NullDigestNotifier</c> is used).
/// </remarks>
public sealed record NotifyOptions
{
    /// <summary>
    /// File to write the rendered digest to after each sweep. Optional — empty/absent means "do not
    /// write a digest file".
    /// </summary>
    public string? DigestPath { get; init; }
}
