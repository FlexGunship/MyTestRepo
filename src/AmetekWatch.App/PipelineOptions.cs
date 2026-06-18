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

    /// <summary>
    /// Retry policy for transient pipeline failures, bound from <c>Pipeline:Retry</c>. Only active
    /// when <see cref="UseRealApi"/> is <c>true</c> — the deterministic fakes never fail, so the fake
    /// tier always uses a no-retry policy regardless of these values.
    /// </summary>
    public RetryOptions Retry { get; init; } = new();
}

/// <summary>
/// Retry knobs bound from <c>Pipeline:Retry</c>. Defaults mirror the shipped config (3 attempts,
/// 500&#8201;ms base backoff) so the section may be omitted.
/// </summary>
public sealed record RetryOptions
{
    /// <summary>Total attempts including the first (so 3 means up to two retries). Minimum 1.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Base backoff in milliseconds; the wait before retry <c>n</c> is <c>BaseDelayMs * 2^(n-1)</c>.</summary>
    public int BaseDelayMs { get; init; } = 500;
}
