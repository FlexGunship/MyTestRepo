namespace AmetekWatch.App;

/// <summary>
/// Configuration for which pipeline tier the host runs, bound from the <c>Pipeline</c> section of
/// <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// The default is <c>false</c> (the deterministic fakes) so the exe runs offline with no key. When
/// <see cref="UseRealApi"/> is <c>true</c> the host builds the real Anthropic adapters — but only if
/// <c>ANTHROPIC_API_KEY</c> is present; otherwise it warns and falls back to the fakes (see
/// <c>Program</c>).
/// </remarks>
public sealed record PipelineOptions
{
    /// <summary>
    /// When <c>true</c> the host runs the real Sonnet&#8201;4.6 search → Opus&#8201;4.8 triage pipeline
    /// (subject to <c>ANTHROPIC_API_KEY</c> being set). When <c>false</c> (the default) it runs the
    /// deterministic fakes.
    /// </summary>
    public bool UseRealApi { get; init; }
}
