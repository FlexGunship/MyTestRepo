using AmetekWatch.Anthropic;
using AmetekWatch.Core.Notify;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Core.Resilience;
using AmetekWatch.Storage;
using Microsoft.Extensions.Configuration;

namespace AmetekWatch.App;

/// <summary>
/// The resolved building blocks of one configured sweep: the selected pipeline tier, the durable
/// store, the digest notifier, the configured <see cref="SweepRunner"/> (retry + new-only +
/// triage-error wiring baked in), the bound options, plus human-readable metadata for the CLI summary.
/// </summary>
public sealed record SweepComposition
{
    public required ISearcher Searcher { get; init; }
    public required ITriageDecider Triage { get; init; }
    public required IFindingStore Store { get; init; }
    public required SweepRunner Runner { get; init; }
    public required IRetryPolicy RetryPolicy { get; init; }
    public required SweepOptions Options { get; init; }
    public required NotifyOptions Notify { get; init; }
    public required IDigestNotifier Notifier { get; init; }
    public required string ActivePipeline { get; init; }
    public required string DbPath { get; init; }
}

/// <summary>
/// Builds a <see cref="SweepComposition"/> from configuration — the single place the 028/032 wiring
/// lives (pipeline-tier selection, SQLite store, digest-sink selection), shared by both the one-shot
/// CLI path and the hosted daemon so neither duplicates the composition.
/// </summary>
/// <remarks>
/// Construction only — it builds objects (and reads the <c>ANTHROPIC_API_KEY</c> presence flag) but
/// invokes no network and prints no secret. The live network call lives in
/// <c>AnthropicMessagesClient</c> and is not exercised here.
/// </remarks>
public static class SweepComposer
{
    /// <summary>
    /// Resolves the composition from <paramref name="config"/>. <paramref name="warn"/> receives a
    /// human-readable message when a requested capability (real API, a digest sink) cannot be
    /// honoured and a safe fallback is used (default no-op).
    /// </summary>
    public static SweepComposition Build(IConfiguration config, Action<string>? warn = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        var log = warn ?? (_ => { });

        var options = config.GetSection("Sweep").Get<SweepOptions>() ?? new SweepOptions();
        var pipeline = config.GetSection("Pipeline").Get<PipelineOptions>() ?? new PipelineOptions();
        var dbPath = config["Storage:DbPath"] ?? "ametek-watch.db";

        // Bind Notify explicitly: EmailOptions (030) is a positional record whose required parameters
        // make the binder throw on an empty/partial Email section. An incomplete email config is the
        // "fall back gracefully" case, so bind it tolerantly rather than letting it crash.
        var notify = new NotifyOptions
        {
            Sink = config["Notify:Sink"] ?? "File",
            DigestPath = config["Notify:DigestPath"],
            Email = TryBindEmail(config.GetSection("Notify:Email")),
        };

        var (searcher, triage, activePipeline) = SelectPipeline(pipeline, log);

        IDigestNotifier notifier = DigestNotifierFactory.Create(
            notify, options.Subject, () => DateTimeOffset.UtcNow, log);

        IFindingStore store = new SqliteFindingStore(dbPath);

        // Resilience (034): retry transient failures only on the real tier — the deterministic fakes
        // never fail, so the fake/fell-back tier uses NoRetryPolicy. New-only digest (038) and
        // per-finding triage isolation (034) are wired here so both the one-shot CLI and the daemon
        // (which share this composition) get identical behaviour.
        IRetryPolicy retryPolicy = pipeline.UseRealApi
            ? new RetryPolicy(
                pipeline.Retry.MaxAttempts,
                TimeSpan.FromMilliseconds(pipeline.Retry.BaseDelayMs),
                AnthropicTransient.IsTransient)
            : new NoRetryPolicy();

        var runner = new SweepRunner(
            searcher,
            triage,
            store,
            retryPolicy,
            onTriageError: (finding, ex) =>
                log($"Triage skipped for {finding.Url}: {ex.GetType().Name}: {ex.Message}"),
            digestOnlyNew: options.OnlyReportNew);

        return new SweepComposition
        {
            Searcher = searcher,
            Triage = triage,
            Store = store,
            Runner = runner,
            RetryPolicy = retryPolicy,
            Options = options,
            Notify = notify,
            Notifier = notifier,
            ActivePipeline = activePipeline,
            DbPath = dbPath,
        };
    }

    private static (ISearcher Searcher, ITriageDecider Triage, string Active) SelectPipeline(
        PipelineOptions pipeline,
        Action<string> log)
    {
        if (!pipeline.UseRealApi)
        {
            var (s, t) = PipelineFactory.Create(useRealApi: false);
            return (s, t, "FAKE (deterministic; Pipeline:UseRealApi=false)");
        }

        var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (hasKey)
        {
            var (s, t) = PipelineFactory.Create(
                useRealApi: true,
                realClientFactory: () => new AnthropicMessagesClient(),
                clock: () => DateTimeOffset.UtcNow);
            return (s, t, "REAL (Sonnet 4.6 web_search -> Opus 4.8 triage)");
        }

        log("WARNING: Pipeline:UseRealApi is true but ANTHROPIC_API_KEY is not set — falling back to the deterministic fakes.");
        var (fs, ft) = PipelineFactory.Create(useRealApi: false);
        return (fs, ft, "FAKE (fell back: ANTHROPIC_API_KEY not set)");
    }

    private static EmailOptions? TryBindEmail(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return null;
        }

        try
        {
            return section.Get<EmailOptions>();
        }
        catch (InvalidOperationException)
        {
            // A partial section (e.g. missing recipients) cannot bind to the required record; treat it
            // as incomplete — DigestNotifierFactory warns and falls back to the no-op sink.
            return null;
        }
    }
}
