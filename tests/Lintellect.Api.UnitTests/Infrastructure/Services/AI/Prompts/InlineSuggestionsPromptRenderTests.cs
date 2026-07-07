using Lintellect.Api.Infrastructure.Services.AI.Prompts;

namespace Lintellect.Api.UnitTests.Infrastructure.Services.AI.Prompts;

/// <summary>
/// Renders the language-specific analysis prompts with the full set of variables the analyzer
/// services supply, and asserts no {{placeholder}} leaks into the final prompt. This guards the
/// class of bug where a template references a variable (e.g. gitProvider) the services never pass.
/// </summary>
[TestFixture]
public class InlineSuggestionsPromptRenderTests
{
    private static readonly EProgrammingLanguage[] SupportedLanguages =
    [
        EProgrammingLanguage.CSharp,
        EProgrammingLanguage.Python,
        EProgrammingLanguage.Java,
        EProgrammingLanguage.JavaScript,
        EProgrammingLanguage.TypeScript
    ];

    // Superset of every variable the analyzer services inject across inline/summary/detailed prompts.
    private static Dictionary<string, string> Variables() => new()
    {
        ["customInstructions"] = "none",
        ["totalFilesInPR"] = "3",
        ["maxSuggestionsPerFile"] = "5",
        ["mcpServers"] = "none",
        ["workItemContext"] = "GOAL: ship it"
    };

    [TestCaseSource(nameof(SupportedLanguages))]
    public void InlineSuggestionsPrompt_HasNoUnresolvedPlaceholders(EProgrammingLanguage language)
    {
        var rendered = new PromptTemplateService().RenderLanguageTemplate(
            LanguagePromptTemplates.InlineSuggestionsSystemPrompt, language, Variables(), enableGlobalInstructions: true);

        rendered.ShouldNotContain("{{");
    }

    [TestCaseSource(nameof(SupportedLanguages))]
    public void SummaryPrompt_HasNoUnresolvedPlaceholders(EProgrammingLanguage language)
    {
        var rendered = new PromptTemplateService().RenderLanguageTemplate(
            LanguagePromptTemplates.SummarySystemPrompt, language, Variables());

        rendered.ShouldNotContain("{{");
    }

    [TestCaseSource(nameof(SupportedLanguages))]
    public void DetailedAnalysisPrompt_HasNoUnresolvedPlaceholders(EProgrammingLanguage language)
    {
        var rendered = new PromptTemplateService().RenderLanguageTemplate(
            LanguagePromptTemplates.DetailedAnalysisSystemPrompt, language, Variables());

        rendered.ShouldNotContain("{{");
    }
}
