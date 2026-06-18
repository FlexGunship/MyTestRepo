using AmetekWatch.Anthropic;
using AmetekWatch.Core.Model;

namespace AmetekWatch.Anthropic.Tests;

/// <summary>
/// Hand-computed oracles for <see cref="TriageVerdictParser"/>: well-formed structured JSON maps to
/// the exact verdict; malformed or incomplete JSON throws rather than coercing.
/// </summary>
public class TriageVerdictParserTests
{
    [Fact]
    public void Parse_KnownJson_MapsToVerdict()
    {
        const string json =
            """{"important":true,"relevant":false,"worthReporting":true,"rationale":"On-point op-ed."}""";

        var verdict = new TriageVerdictParser().Parse(json);

        Assert.Equal(
            new TriageVerdict(Important: true, Relevant: false, WorthReporting: true, Rationale: "On-point op-ed."),
            verdict);
    }

    [Fact]
    public void Parse_Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => new TriageVerdictParser().Parse("not json at all"));
    }

    [Fact]
    public void Parse_MissingField_Throws()
    {
        // 'worthReporting' absent — a partial verdict must not be silently fabricated.
        const string json = """{"important":true,"relevant":true,"rationale":"x"}""";

        Assert.Throws<FormatException>(() => new TriageVerdictParser().Parse(json));
    }

    [Fact]
    public void Parse_WrongType_Throws()
    {
        // 'important' is a string, not a boolean.
        const string json =
            """{"important":"yes","relevant":true,"worthReporting":true,"rationale":"x"}""";

        Assert.Throws<FormatException>(() => new TriageVerdictParser().Parse(json));
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TriageVerdictParser().Parse(null!));
    }
}
