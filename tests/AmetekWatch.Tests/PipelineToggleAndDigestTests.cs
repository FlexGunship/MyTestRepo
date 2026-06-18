using AmetekWatch.Anthropic;
using AmetekWatch.App;
using AmetekWatch.Core.Notify;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Storage;
using Anthropic.Models.Messages;
using Microsoft.Data.Sqlite;

namespace AmetekWatch.Tests;

/// <summary>
/// Capstone (028) tests: the App selects real-vs-fake by config and emits the digest after a sweep.
///
/// Two independent concerns, both offline (no network, no API key):
///   1. <b>Selection</b> — <see cref="PipelineFactory.Create"/> resolves the REAL Anthropic adapter
///      runtime types when <c>useRealApi=true</c> and the fakes when <c>false</c>. The real path is
///      handed a fake <see cref="IMessagesClient"/> that is NEVER invoked, so no key is required and
///      no call is made — only the resolved types are asserted.
///   2. <b>Digest wiring</b> — one fake sweep over a temp-file SQLite DB persists the findings AND
///      writes the digest file at the configured path with real content.
///
/// Digest oracle (from <see cref="FakeSearcher.Canned"/>, hand-computed in SweepHostTests): 4 unique
/// persisted, 3 worth-reporting → the file announces "3 items worth reporting."
/// </summary>
public sealed class PipelineToggleAndDigestTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"ametek-watch-028-{Guid.NewGuid():N}.db");

    private readonly string _digestPath = Path.Combine(
        Path.GetTempPath(),
        $"ametek-watch-028-{Guid.NewGuid():N}.md");

    /// <summary>A fake messages client that must never be invoked by these tests.</summary>
    private sealed class UnusedMessagesClient : IMessagesClient
    {
        public Task<string> CreateMessageTextAsync(MessageCreateParams parameters, CancellationToken ct) =>
            throw new InvalidOperationException("the messages client must not be invoked by a type-selection test");
    }

    [Fact]
    public void Create_UseRealApiTrue_ResolvesRealAnthropicTypes_WithoutInvokingThem()
    {
        var (searcher, triage) = PipelineFactory.Create(
            useRealApi: true,
            realClientFactory: () => new UnusedMessagesClient(),
            clock: () => DateTimeOffset.UtcNow);

        // Assert the RESOLVED runtime types only — no SweepAsync/JudgeAsync call, so no network/key.
        Assert.IsType<AnthropicSearcher>(searcher);
        Assert.IsType<AnthropicTriageDecider>(triage);
    }

    [Fact]
    public void Create_UseRealApiFalse_ResolvesFakes()
    {
        var (searcher, triage) = PipelineFactory.Create(useRealApi: false);

        Assert.IsType<FakeSearcher>(searcher);
        Assert.IsType<FakeTriageDecider>(triage);
    }

    [Fact]
    public async Task RunOnce_WithDigestPath_PersistsToSqlite_AndWritesDigestFile()
    {
        var (searcher, triage) = PipelineFactory.Create(useRealApi: false);
        var store = new SqliteFindingStore(_dbPath);
        var options = new SweepOptions { Subject = "AMETEK", RunOnce = true };
        var notifier = new FileDigestNotifier(_digestPath, options.Subject, () => DateTimeOffset.UtcNow);
        var host = new SweepHost(searcher, triage, store, options, notifier);

        var digest = await host.RunOnceAsync(CancellationToken.None);
        var persisted = await store.GetAllAsync(CancellationToken.None);

        // Persisted to SQLite: 4 unique after URL-dedupe, 3 worth-reporting (the digest).
        Assert.Equal(4, persisted.Count);
        Assert.Equal(3, digest.Count);

        // The digest file exists and carries real content (heading + the hand-computed count line).
        Assert.True(File.Exists(_digestPath));
        var contents = await File.ReadAllTextAsync(_digestPath);
        Assert.Contains("AMETEK Watch digest", contents);
        Assert.Contains("3 items worth reporting.", contents);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        if (File.Exists(_digestPath))
        {
            File.Delete(_digestPath);
        }
    }
}
