using AmetekWatch.Anthropic;
using AmetekWatch.App;
using AmetekWatch.Core.Notify;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Microsoft.Extensions.Configuration;

// AMETEK Watch — config-driven sweep host (capstone).
//
// Binds appsettings.json, selects the pipeline tier (real Sonnet 4.6 -> Opus 4.8 when
// Pipeline:UseRealApi is true AND ANTHROPIC_API_KEY is present, else the deterministic fakes),
// constructs a durable SQLite store, runs one sweep (RunOnce default true so the CLI terminates),
// writes the worth-reporting digest to Notify:DigestPath when set, and prints the digest.
//
// No API key is ever read or printed here beyond a presence check; the live network call lives in
// AnthropicMessagesClient and is not exercised offline.

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var options = config.GetSection("Sweep").Get<SweepOptions>() ?? new SweepOptions();
var pipeline = config.GetSection("Pipeline").Get<PipelineOptions>() ?? new PipelineOptions();
var dbPath = config["Storage:DbPath"] ?? "ametek-watch.db";

// Bind Notify explicitly: EmailOptions (030) is a positional record whose required parameters make
// the binder throw on an empty/partial Email section. An incomplete email config is exactly the
// "fall back gracefully" case (Decision 2), so we bind it tolerantly here rather than letting it crash.
var notify = new NotifyOptions
{
    Sink = config["Notify:Sink"] ?? "File",
    DigestPath = config["Notify:DigestPath"],
    Email = TryBindEmail(config.GetSection("Notify:Email")),
};

static AmetekWatch.Core.Notify.EmailOptions? TryBindEmail(IConfigurationSection section)
{
    if (!section.Exists())
    {
        return null;
    }

    try
    {
        return section.Get<AmetekWatch.Core.Notify.EmailOptions>();
    }
    catch (InvalidOperationException)
    {
        // A partial section (e.g. missing recipients) cannot bind to the required record; treat it as
        // an incomplete config — DigestNotifierFactory will warn and fall back to the no-op sink.
        return null;
    }
}

// --- Select the pipeline tier. Real adapters only when configured AND a key is present; otherwise
//     warn (if asked for real) and fall back to the fakes so the exe still runs and demonstrates.
ISearcher searcher;
ITriageDecider triage;
string activePipeline;

if (pipeline.UseRealApi)
{
    var hasKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
    if (hasKey)
    {
        (searcher, triage) = PipelineFactory.Create(
            useRealApi: true,
            realClientFactory: () => new AnthropicMessagesClient(),
            clock: () => DateTimeOffset.UtcNow);
        activePipeline = "REAL (Sonnet 4.6 web_search -> Opus 4.8 triage)";
    }
    else
    {
        Console.WriteLine(
            "WARNING: Pipeline:UseRealApi is true but ANTHROPIC_API_KEY is not set — falling back to the deterministic fakes.");
        (searcher, triage) = PipelineFactory.Create(useRealApi: false);
        activePipeline = "FAKE (fell back: ANTHROPIC_API_KEY not set)";
    }
}
else
{
    (searcher, triage) = PipelineFactory.Create(useRealApi: false);
    activePipeline = "FAKE (deterministic; Pipeline:UseRealApi=false)";
}

// --- Digest sink: selected by Notify:Sink (File / Email / None). An incomplete/disabled email
//     config (or an unrecognized sink) warns and falls back to the no-op sink — no crash.
IDigestNotifier notifier = DigestNotifierFactory.Create(
    notify, options.Subject, () => DateTimeOffset.UtcNow, Console.WriteLine);

var store = new SqliteFindingStore(dbPath);
var host = new SweepHost(searcher, triage, store, options, notifier);

var digest = await host.RunOnceAsync(CancellationToken.None);
var persisted = await store.GetAllAsync(CancellationToken.None);

Console.WriteLine($"AMETEK Watch — sweep for \"{options.Subject}\"");
Console.WriteLine($"Pipeline:               {activePipeline}");
Console.WriteLine($"Store (SQLite):         {dbPath}");
Console.WriteLine($"Digest sink:            {notify.Sink} -> {notifier.GetType().Name}");
Console.WriteLine($"Persisted findings:     {persisted.Count}");
Console.WriteLine($"Worth-reporting digest: {digest.Count}");
Console.WriteLine();

if (digest.Count == 0)
{
    Console.WriteLine("(nothing worth reporting)");
}
else
{
    var n = 1;
    foreach (var item in digest)
    {
        Console.WriteLine($"[{n}] {item.Finding.Category} — {item.Finding.Title}");
        Console.WriteLine($"    url:       {item.Finding.Url}");
        Console.WriteLine($"    rationale: {item.Verdict.Rationale}");
        n++;
    }
}

return 0;
