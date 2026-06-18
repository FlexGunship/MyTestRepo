using System.Net;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;

// AMETEK Watch — local web-UI dashboard (read-only).
//
// Browses triaged findings from an IFindingStore. Until the durable SQLite store (spec 007)
// lands, the store is seeded once at startup by running a single fake sweep through Core
// (FakeSearcher + FakeTriageDecider + SweepRunner) — no Anthropic SDK, no network, no API key.
// A later spec swaps the registration to the SQLite store behind this same seam.

var builder = WebApplication.CreateBuilder(args);

// Read-only and local: bind loopback only, no auth.
builder.WebHost.UseUrls("http://localhost:5080");

// Seed the store with one fake sweep. SweepRunner persists EVERY triaged survivor (the digest
// it returns is just the worth-reporting subset); the dashboard shows everything persisted.
var store = new InMemoryFindingStore();
var runner = new SweepRunner(new FakeSearcher(), new FakeTriageDecider(), store);
await runner.RunAsync(new SweepQuery(Subject: "AMETEK"), CancellationToken.None);

builder.Services.AddSingleton<IFindingStore>(store);

var app = builder.Build();

// GET /api/findings — JSON array of all triaged findings, most-recent DiscoveredAt first.
app.MapGet("/api/findings", async (IFindingStore findings, CancellationToken ct) =>
{
    var all = await findings.GetAllAsync(ct);
    var ordered = all
        .OrderByDescending(t => t.Finding.DiscoveredAt)
        .ToList();
    return Results.Json(ordered);
});

// GET / — minimal self-contained HTML table of findings (server-rendered).
app.MapGet("/", async (IFindingStore findings, CancellationToken ct) =>
{
    var all = await findings.GetAllAsync(ct);
    var ordered = all
        .OrderByDescending(t => t.Finding.DiscoveredAt)
        .ToList();
    return Results.Content(RenderHtml(ordered), "text/html");
});

app.Run();

// Renders the findings table as a self-contained HTML document. All dynamic text is
// HTML-encoded to keep the page safe against the (canned, but on principle) source data.
static string RenderHtml(IReadOnlyList<TriagedFinding> findings)
{
    static string E(string s) => WebUtility.HtmlEncode(s);

    var rows = new System.Text.StringBuilder();
    foreach (var t in findings)
    {
        var worth = t.Verdict.WorthReporting ? "yes" : "no";
        rows.Append("        <tr>")
            .Append("<td>").Append(E(t.Finding.Category.ToString())).Append("</td>")
            .Append("<td>").Append(E(t.Finding.Title)).Append("</td>")
            .Append("<td><a href=\"").Append(E(t.Finding.Url)).Append("\">")
                .Append(E(t.Finding.Url)).Append("</a></td>")
            .Append("<td>").Append(worth).Append("</td>")
            .Append("<td>").Append(E(t.Verdict.Rationale)).Append("</td>")
            .Append("</tr>\n");
    }

    return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <title>AMETEK Watch — findings</title>
          <style>
            body { font-family: system-ui, sans-serif; margin: 2rem; }
            table { border-collapse: collapse; width: 100%; }
            th, td { border: 1px solid #ccc; padding: 6px 10px; text-align: left; vertical-align: top; }
            th { background: #f3f3f3; }
          </style>
        </head>
        <body>
          <h1>AMETEK Watch — findings ({{findings.Count}})</h1>
          <table>
            <thead>
              <tr><th>Category</th><th>Title</th><th>URL</th><th>Worth reporting</th><th>Rationale</th></tr>
            </thead>
            <tbody>
        {{rows}}    </tbody>
          </table>
        </body>
        </html>
        """;
}

// Exposed so the test project's WebApplicationFactory<Program> can reference the entry point.
public partial class Program { }
