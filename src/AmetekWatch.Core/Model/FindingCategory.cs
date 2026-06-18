namespace AmetekWatch.Core.Model;

/// <summary>
/// Coarse classification of a finding, assigned by the searcher tier. The triage
/// tier uses it (among other signals) to decide whether a finding is worth reporting.
/// </summary>
public enum FindingCategory
{
    /// <summary>Opinion, commentary, or social-media chatter about AMETEK.</summary>
    OpinionSocial,

    /// <summary>A financial report, earnings item, or filing.</summary>
    FinancialReport,

    /// <summary>Anything that does not fit the more specific buckets.</summary>
    Other,
}
