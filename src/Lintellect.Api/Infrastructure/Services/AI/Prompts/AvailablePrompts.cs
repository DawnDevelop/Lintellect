namespace Lintellect.Api.Infrastructure.Services.AI.Prompts;

internal enum LanguagePromptTemplates
{
    DetailedAnalysisSystemPrompt,
    InlineSuggestionsSystemPrompt,
    SummarySystemPrompt,
}

internal enum GeneralPromptTemplates
{
    CodeOwnerSystemPrompt,
    GlobalInstructionsPrompt,
    QuestionAnsweringPrompt,
    WorkItemSummarizerSystemPrompt
}

internal static class AvailablePrompts
{

    internal static readonly Dictionary<LanguagePromptTemplates, string> LanguagePrompts = new()
    {
        { LanguagePromptTemplates.DetailedAnalysisSystemPrompt, "DetailedAnalysisSystemPrompt" },
        { LanguagePromptTemplates.InlineSuggestionsSystemPrompt, "InlineSuggestionsSystemPrompt" },
        { LanguagePromptTemplates.SummarySystemPrompt, "SummarySystemPrompt" },
    };

    internal static readonly Dictionary<GeneralPromptTemplates, string> GeneralPrompts = new()
    {
        { GeneralPromptTemplates.CodeOwnerSystemPrompt, "CodeOwnerSystemPrompt" },
        { GeneralPromptTemplates.GlobalInstructionsPrompt, "GlobalInstructionsPrompt" },
        { GeneralPromptTemplates.QuestionAnsweringPrompt, "QuestionAnsweringPrompt" },
        { GeneralPromptTemplates.WorkItemSummarizerSystemPrompt, "WorkItemSummarizerSystemPrompt" }
    };
}
