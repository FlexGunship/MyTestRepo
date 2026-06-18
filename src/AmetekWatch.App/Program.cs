using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

// AMETEK Watch — vertical-slice console host.
//
// Wires the orchestrator from deterministic fakes (no Anthropic SDK, no network, no API key),
// runs one sweep for "AMETEK", and prints the digest. This host is what later becomes the
// Windows service / UI host once the real pipeline tiers are wired in.

var store = new InMemoryFindingStore();
var runner = new SweepRunner(new FakeSearcher(), new FakeTriageDecider(), store);
var query = new SweepQuery(Subject: "AMETEK");

var digest = await runner.RunAsync(query, CancellationToken.None);
var persisted = await store.GetAllAsync(CancellationToken.None);

Console.WriteLine($"AMETEK Watch — sweep for \"{query.Subject}\"");
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
