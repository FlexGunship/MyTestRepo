namespace AmetekWatch.App;

/// <summary>
/// Configuration for one sweep host run, bound from the <c>Sweep</c> section of
/// <c>appsettings.json</c>. Defaults mirror the shipped config so the host is usable even if a
/// section is missing.
/// </summary>
/// <remarks>
/// The storage location (<c>Storage:DbPath</c>) lives in a separate config section because it is
/// consumed by the host's composition (constructing the store), not by <see cref="SweepHost"/>'s
/// own loop logic.
/// </remarks>
public sealed record SweepOptions
{
    /// <summary>The subject to sweep for, e.g. <c>"AMETEK"</c>.</summary>
    public string Subject { get; init; } = "AMETEK";

    /// <summary>Minutes to wait between sweeps when <see cref="RunOnce"/> is <c>false</c>.</summary>
    public int IntervalMinutes { get; init; } = 1440;

    /// <summary>
    /// When <c>true</c> the host runs exactly one sweep and returns (the CLI default, so the exe
    /// terminates deterministically). When <c>false</c> it loops with <see cref="IntervalMinutes"/>
    /// spacing until cancelled.
    /// </summary>
    public bool RunOnce { get; init; } = true;

    /// <summary>
    /// When <c>true</c> the digest reports only worth-reporting findings whose URL was not already in
    /// the store before the sweep (spec 038's <c>digestOnlyNew</c>), so a periodic daemon stops
    /// re-notifying findings it has already reported. Default <c>false</c> keeps the CLI/tests
    /// deterministic (the digest is the full worth-reporting subset of each sweep).
    /// </summary>
    public bool OnlyReportNew { get; init; }
}
