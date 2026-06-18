using System.Text.Json;
using AmetekWatch.Core.Model;
using AmetekWatch.Core.Triage;
using Anthropic.Models.Messages;

namespace AmetekWatch.Anthropic;

/// <summary>
/// Pure builder that turns a <see cref="Finding"/> into a fully-formed <see cref="MessageCreateParams"/>
/// for the Opus 4.8 triage call — no I/O, no model call. It composes the 011 prompt logic
/// (<see cref="TriagePromptBuilder"/>) with the SDK's request shape:
/// <list type="bullet">
///   <item>Model <c>claude-opus-4-8</c>.</item>
///   <item>The stable rubric as a single cached system block (<see cref="CacheControlEphemeral"/>) so the
///   rubric prefix is prompt-cached across findings — the charter's cost lever.</item>
///   <item>The rendered finding as the user message.</item>
///   <item>Structured output: a JSON schema pinning the four verdict fields, all required,
///   <c>additionalProperties:false</c>.</item>
/// </list>
/// </summary>
public sealed class TriageRequestFactory
{
    /// <summary>Output cap for a triage verdict — a few booleans plus a short rationale.</summary>
    private const long MaxTokens = 1024;

    /// <summary>
    /// Builds the triage request for <paramref name="finding"/>. Pure: same finding in, byte-identical
    /// params out. Throws <see cref="ArgumentNullException"/> if <paramref name="finding"/> is null.
    /// </summary>
    public MessageCreateParams Build(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        return new MessageCreateParams
        {
            Model = Model.ClaudeOpus4_8,
            MaxTokens = MaxTokens,
            System = new List<TextBlockParam>
            {
                new()
                {
                    Text = TriagePromptBuilder.BuildSystemPrompt(),
                    CacheControl = new CacheControlEphemeral(),
                },
            },
            Messages = new List<MessageParam>
            {
                new()
                {
                    Role = Role.User,
                    Content = TriagePromptBuilder.BuildUserContent(finding),
                },
            },
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = BuildSchema() },
            },
        };
    }

    /// <summary>
    /// The structured-output schema: <c>{ important, relevant, worthReporting : boolean,
    /// rationale : string }</c> — all four required, <c>additionalProperties:false</c>.
    /// </summary>
    private static Dictionary<string, JsonElement> BuildSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            important = new { type = "boolean" },
            relevant = new { type = "boolean" },
            worthReporting = new { type = "boolean" },
            rationale = new { type = "string" },
        }),
        ["required"] = JsonSerializer.SerializeToElement(
            new[] { "important", "relevant", "worthReporting", "rationale" }),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };
}
