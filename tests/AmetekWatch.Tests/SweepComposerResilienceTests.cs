using AmetekWatch.App;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Pipeline;
using AmetekWatch.Core.Resilience;
using Microsoft.Extensions.Configuration;

namespace AmetekWatch.Tests;

/// <summary>
/// Spec 041 tests for <see cref="SweepComposer.Build"/>'s resilience wiring, all offline (the FAKE
/// tier — no network, no <c>ANTHROPIC_API_KEY</c>):
/// <list type="bullet">
///   <item>The fake tier resolves a <see cref="NoRetryPolicy"/> (retry is real-tier only).</item>
///   <item><c>Sweep:OnlyReportNew</c> is honored end-to-end through the composed
///   <see cref="SweepRunner"/> — true suppresses re-reporting of findings already in the store,
///   false reports the full worth-reporting digest every run.</item>
/// </list>
/// </summary>
public sealed class SweepComposerResilienceTests
{
    private static IConfiguration Config(string dbPath, bool useRealApi, bool onlyReportNew) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sweep:Subject"] = "AMETEK",
                ["Sweep:RunOnce"] = "true",
                ["Sweep:OnlyReportNew"] = onlyReportNew ? "true" : "false",
                ["Storage:DbPath"] = dbPath,
                ["Pipeline:UseRealApi"] = useRealApi ? "true" : "false",
                // Real-tier retry knobs — present but inert on the fake tier.
                ["Pipeline:Retry:MaxAttempts"] = "3",
                ["Pipeline:Retry:BaseDelayMs"] = "500",
                ["Notify:Sink"] = "None",
            })
            .Build();

    private static string TempDb() =>
        Path.Combine(Path.GetTempPath(), $"ametek-composer-{Guid.NewGuid():N}.db");

    [Fact]
    public void FakeTier_ResolvesNoRetryPolicy()
    {
        var db = TempDb();
        try
        {
            var composition = SweepComposer.Build(Config(db, useRealApi: false, onlyReportNew: false));

            // The fakes never fail, so the fake/fell-back tier opts out of retry. (Asserting the
            // selected policy directly, since the SweepRunner doesn't expose its policy.)
            Assert.IsType<NoRetryPolicy>(composition.RetryPolicy);
            Assert.Contains("FAKE", composition.ActivePipeline);
            Assert.IsType<FakeSearcher>(composition.Searcher);
            Assert.IsType<FakeTriageDecider>(composition.Triage);
        }
        finally
        {
            File.Delete(db);
        }
    }

    [Fact]
    public async Task OnlyReportNewTrue_SecondRunOverSameStoreReportsNothing()
    {
        var db = TempDb();
        try
        {
            // First run: every worth-reporting finding is new → full digest of 3 (the fakes yield
            // 4 unique findings, 3 of them worth-reporting).
            var first = SweepComposer.Build(Config(db, useRealApi: false, onlyReportNew: true));
            var firstDigest = await first.Runner.RunAsync(new SweepQuery(first.Options.Subject));
            Assert.Equal(3, firstDigest.Count);

            // Second run over the SAME store (fresh composition): all URLs are now known, so an
            // only-new digest reports nothing — proving digestOnlyNew is wired from OnlyReportNew.
            var second = SweepComposer.Build(Config(db, useRealApi: false, onlyReportNew: true));
            var secondDigest = await second.Runner.RunAsync(new SweepQuery(second.Options.Subject));
            Assert.Empty(secondDigest);
        }
        finally
        {
            File.Delete(db);
        }
    }

    [Fact]
    public async Task OnlyReportNewFalse_SecondRunStillReportsFullDigest()
    {
        var db = TempDb();
        try
        {
            var first = SweepComposer.Build(Config(db, useRealApi: false, onlyReportNew: false));
            var firstDigest = await first.Runner.RunAsync(new SweepQuery(first.Options.Subject));
            Assert.Equal(3, firstDigest.Count);

            // OnlyReportNew=false (the default): the digest is the full worth-reporting subset every
            // run, regardless of what the store already holds.
            var second = SweepComposer.Build(Config(db, useRealApi: false, onlyReportNew: false));
            var secondDigest = await second.Runner.RunAsync(new SweepQuery(second.Options.Subject));
            Assert.Equal(3, secondDigest.Count);
        }
        finally
        {
            File.Delete(db);
        }
    }
}
