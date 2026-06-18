using AmetekWatch.App;
using AmetekWatch.Core.Notify;

namespace AmetekWatch.Tests;

/// <summary>
/// Spec 032 tests: <see cref="DigestNotifierFactory.Create"/> resolves the right
/// <see cref="IDigestNotifier"/> by <see cref="NotifyOptions.Sink"/> — purely by construction,
/// invoking nothing (no SMTP send, no network, no file written). Only the resolved runtime TYPE is
/// asserted; the Email path constructs (but never calls) the live <c>SmtpEmailSender</c>.
/// </summary>
public sealed class DigestSinkSelectionTests
{
    private static readonly Func<DateTimeOffset> Clock = () => DateTimeOffset.UtcNow;

    private static EmailOptions CompleteEmail() => new EmailOptions(
        Enabled: true,
        SmtpHost: "smtp.example.com",
        SmtpPort: 587,
        From: "watch@example.com",
        To: new[] { "owner@example.com" },
        SubjectPrefix: "AMETEK Watch");

    [Fact]
    public void File_WithPath_ResolvesFileNotifier()
    {
        var notify = new NotifyOptions { Sink = "File", DigestPath = "digest.md" };

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock);

        Assert.IsType<FileDigestNotifier>(notifier);
    }

    [Fact]
    public void File_WithoutPath_FallsBackToNull_AndWarns()
    {
        var notify = new NotifyOptions { Sink = "File", DigestPath = "" };
        var warnings = new List<string>();

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock, warnings.Add);

        Assert.IsType<NullDigestNotifier>(notifier);
        Assert.Single(warnings);
    }

    [Fact]
    public void Email_Complete_ResolvesEmailNotifier_WithoutSending()
    {
        var notify = new NotifyOptions { Sink = "Email", Email = CompleteEmail() };

        // Construction only — no SendAsync is called, so no SMTP/network happens here.
        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock);

        Assert.IsType<EmailDigestNotifier>(notifier);
    }

    [Fact]
    public void Email_Disabled_FallsBackToNull_AndWarns()
    {
        var notify = new NotifyOptions { Sink = "Email", Email = CompleteEmail() with { Enabled = false } };
        var warnings = new List<string>();

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock, warnings.Add);

        Assert.IsType<NullDigestNotifier>(notifier);
        Assert.Single(warnings);
    }

    [Fact]
    public void Email_IncompleteHost_FallsBackToNull_AndWarns()
    {
        var notify = new NotifyOptions { Sink = "Email", Email = CompleteEmail() with { SmtpHost = "" } };
        var warnings = new List<string>();

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock, warnings.Add);

        Assert.IsType<NullDigestNotifier>(notifier);
        Assert.Single(warnings);
    }

    [Fact]
    public void Email_NoConfig_FallsBackToNull()
    {
        var notify = new NotifyOptions { Sink = "Email", Email = null };

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock);

        Assert.IsType<NullDigestNotifier>(notifier);
    }

    [Fact]
    public void None_ResolvesNullNotifier()
    {
        var notify = new NotifyOptions { Sink = "None", DigestPath = "digest.md" };

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock);

        Assert.IsType<NullDigestNotifier>(notifier);
    }

    [Fact]
    public void Unknown_Sink_FallsBackToNull_AndWarns()
    {
        var notify = new NotifyOptions { Sink = "Carrier Pigeon", DigestPath = "digest.md" };
        var warnings = new List<string>();

        var notifier = DigestNotifierFactory.Create(notify, "AMETEK", Clock, warnings.Add);

        Assert.IsType<NullDigestNotifier>(notifier);
        Assert.Single(warnings);
    }
}
