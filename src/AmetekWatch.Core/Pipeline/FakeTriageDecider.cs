using AmetekWatch.Core.Model;

namespace AmetekWatch.Core.Pipeline;

/// <summary>
/// Deterministic stand-in for the real (Opus 4.8) triage tier. Renders a verdict purely from
/// the finding's <see cref="FindingCategory"/> by a fixed, stated rule — no I/O, no model call:
/// <list type="bullet">
///   <item><see cref="FindingCategory.OpinionSocial"/> and
///   <see cref="FindingCategory.FinancialReport"/> → <c>WorthReporting = true</c>.</item>
///   <item><see cref="FindingCategory.Other"/> → <c>WorthReporting = false</c>.</item>
/// </list>
/// Every category is treated as <c>Relevant</c> (the searcher already scoped to the subject);
/// <c>Important</c> mirrors <c>WorthReporting</c> for this fake.
/// </summary>
public sealed class FakeTriageDecider : ITriageDecider
{
    public Task<TriageVerdict> JudgeAsync(Finding finding, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var worth = finding.Category is FindingCategory.OpinionSocial
            or FindingCategory.FinancialReport;

        var rationale = worth
            ? $"Category {finding.Category} is reportable under the slice rule."
            : $"Category {finding.Category} is not reportable under the slice rule.";

        var verdict = new TriageVerdict(
            Important: worth,
            Relevant: true,
            WorthReporting: worth,
            Rationale: rationale);

        return Task.FromResult(verdict);
    }
}
