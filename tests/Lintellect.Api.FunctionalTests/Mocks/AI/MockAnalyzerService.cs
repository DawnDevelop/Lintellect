namespace Lintellect.Api.FunctionalTests.Mocks.AI;


/// <summary>
/// Mock implementation of IAnalyzerService for testing.
/// </summary>
public sealed class MockAnalyzerService : IAnalyzerService
{
    public string? LastDetailedWorkItemContext { get; private set; }
    public string? LastSummaryWorkItemContext { get; private set; }
    public string? LastInlineWorkItemGoal { get; private set; }
    public int SummarizeContextCallCount { get; private set; }

    public Task<string> GetDetailedAnalysisAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        LastDetailedWorkItemContext = analysisResult.WorkItemContext;
        return Task.FromResult("Mock detailed analysis");
    }

    public Task<string> GenerateSummaryAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        LastSummaryWorkItemContext = analysisResult.WorkItemContext;
        return Task.FromResult("Mock summary");
    }

    public Task<string> SummarizeContextAsync(string systemPrompt, string userPrompt, int maxOutputTokens, CancellationToken cancellationToken = default)
    {
        SummarizeContextCallCount++;
        return Task.FromResult("GOAL: Implement the linked work item.\n\nCONTEXT:\nThe linked item asks for X to be done.");
    }

    public Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        LastInlineWorkItemGoal = analysisResult.WorkItemGoal;
        return Task.FromResult(new List<InlineSuggestion>
        {
            new()
            {
                FilePath = "TestFile.cs",
                LineFrom = 10,
                Title = "Mock suggestion title",
                Explanation = "Mock suggestion explanation",
                SuggestedCode = "Mock suggested code"
            }
        });
    }

    public Task<CodeOwnersResult?> GetCodeOwnersAsync(string codeOwnerFileContent, List<string> changedFilePaths, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CodeOwnersResult?>(new CodeOwnersResult
        {
            CodeOwners =
            [
                new() { Name = "test@example.com", Type = CodeOwnerType.Email, Email = "test@example.com" }
            ]
        });
    }

    public Task<string> AnswerQuestionAsync(AnalyzerServiceModel analysisResult, string threadContext, string question, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Mock answer to question");
    }
}
