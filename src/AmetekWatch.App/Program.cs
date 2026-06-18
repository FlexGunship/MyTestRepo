using AmetekWatch.App;
using AmetekWatch.Core.Notify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// AMETEK Watch — config-driven sweep host.
//
// Sweep:RunOnce=true (default) → run ONE sweep and exit 0 (deterministic CLI; the original
// behaviour). Sweep:RunOnce=false → run as a long-lived Generic-Host daemon: a
// SweepBackgroundService drives the existing interval loop and Ctrl+C / SIGTERM stops it
// gracefully. Both paths share SweepComposer for the 028/032 pipeline+store+digest wiring.
//
// No API key is ever read or printed here beyond a presence check; the live network call lives in
// AnthropicMessagesClient and is not exercised offline.

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var composition = SweepComposer.Build(config, Console.WriteLine);

if (composition.Options.RunOnce)
{
    return await RunOnceAsync(composition);
}

return RunDaemon(args, composition);

// --- One-shot: run a single sweep, persist, deliver the digest, print a summary, exit 0. ---
static async Task<int> RunOnceAsync(SweepComposition c)
{
    var host = new SweepHost(c.Searcher, c.Triage, c.Store, c.Options, c.Notifier, c.Runner);

    var digest = await host.RunOnceAsync(CancellationToken.None);
    var persisted = await c.Store.GetAllAsync(CancellationToken.None);

    Console.WriteLine($"AMETEK Watch — sweep for \"{c.Options.Subject}\"");
    Console.WriteLine($"Pipeline:               {c.ActivePipeline}");
    Console.WriteLine($"Store (SQLite):         {c.DbPath}");
    Console.WriteLine($"Digest sink:            {c.Notify.Sink} -> {c.Notifier.GetType().Name}");
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
}

// --- Daemon: build a Generic Host, register the sweep service, Run() until Ctrl+C / SIGTERM. ---
static int RunDaemon(string[] args, SweepComposition c)
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSingleton(c.Options);

    // Build the SweepHost inside the host's DI from the composed pipeline/store/digest wiring.
    // The digest notifier is wrapped so each completed sweep is logged through the host logger.
    builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AmetekWatch.Sweep");
        IDigestNotifier notifier = new LoggingDigestNotifier(c.Notifier, logger);
        return new SweepHost(c.Searcher, c.Triage, c.Store, c.Options, notifier, c.Runner);
    });

    builder.Services.AddHostedService<SweepBackgroundService>();

    using var host = builder.Build();

    var startupLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AmetekWatch.App");
    startupLogger.LogInformation(
        "AMETEK Watch daemon: subject \"{Subject}\", pipeline {Pipeline}, store {Db}, digest sink {Sink}.",
        c.Options.Subject, c.ActivePipeline, c.DbPath, c.Notify.Sink);

    host.Run();
    return 0;
}
