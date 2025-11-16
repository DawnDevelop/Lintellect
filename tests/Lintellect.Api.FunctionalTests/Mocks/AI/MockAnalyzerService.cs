namespace Lintellect.Api.FunctionalTests.Mocks.AI;


/// <summary>
/// Mock implementation of IAnalyzerServiceResolver for testing.
/// </summary>
public sealed class MockAnalyzerServiceResolver : IAnalyzerServiceResolver
{
    public IAnalyzerService GetAnalyzerService(EAnalyzers provider)
    {
        return new MockAnalyzerService();
    }
}

/// <summary>
/// Mock implementation of IAnalyzerService for testing.
/// </summary>
public sealed class MockAnalyzerService : IAnalyzerService
{
    public Task<string> GetDetailedAnalysisAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Mock detailed analysis");
    }

    public Task<string> GenerateSummaryAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Mock summary");
    }

    public Task<List<InlineSuggestion>> GenerateInlineSuggestionsAsync(AnalyzerServiceModel analysisResult, Dictionary<string, string> diffs, CancellationToken cancellationToken = default)
    {
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
