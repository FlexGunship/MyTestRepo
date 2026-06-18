using AmetekWatch.App;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Microsoft.Extensions.Configuration;

// AMETEK Watch — config-driven sweep host.
//
// Binds appsettings.json -> SweepOptions, constructs a durable SQLite store plus the deterministic
// fakes (no Anthropic SDK, no network, no API key — the real pipeline tiers are the final deferred
// spec), runs one sweep (RunOnce default true so the CLI terminates), and prints the digest.

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var options = config.GetSection("Sweep").Get<SweepOptions>() ?? new SweepOptions();
var dbPath = config["Storage:DbPath"] ?? "ametek-watch.db";

var store = new SqliteFindingStore(dbPath);
var host = new SweepHost(new FakeSearcher(), new FakeTriageDecider(), store, options);

var digest = await host.RunOnceAsync(CancellationToken.None);
var persisted = await store.GetAllAsync(CancellationToken.None);

Console.WriteLine($"AMETEK Watch — sweep for \"{options.Subject}\"");
Console.WriteLine($"Store (SQLite):         {dbPath}");
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
