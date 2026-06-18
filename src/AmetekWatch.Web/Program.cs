using System.Net;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;

// AMETEK Watch — local web-UI dashboard (read-only).
//
// Browses triaged findings from the shared durable SQLite store (spec 017): the same database
// the sweep host (spec 015) persists to, so the dashboard displays what the sweeper actually
// wrote. The DB path comes from config (Storage:DbPath, default ametek-watch.db, matching the
// App's appsettings.json). SqliteFindingStore creates its schema on init, so a missing/empty DB
// yields an empty dashboard rather than a crash. Still offline — no Anthropic SDK, no network,
// no API key.

var builder = WebApplication.CreateBuilder(args);

// Read-only and local: bind loopback only, no auth.
builder.WebHost.UseUrls("http://localhost:5080");

// Read the durable SQLite store from config and register it behind the IFindingStore seam.
// Schema-on-init means a fresh/empty DB serves [] rather than crashing.
var dbPath = builder.Configuration["Storage:DbPath"] ?? "ametek-watch.db";
builder.Services.AddSingleton<IFindingStore>(new SqliteFindingStore(dbPath));

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
