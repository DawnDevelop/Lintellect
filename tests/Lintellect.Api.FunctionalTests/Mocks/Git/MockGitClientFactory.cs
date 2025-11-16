namespace Lintellect.Api.FunctionalTests.Mocks.Git;

/// <summary>
/// Mock implementation of IGitClientFactory for testing.
/// </summary>
public sealed class MockGitClientFactory : IGitClientFactory
{
    public IGitClient CreateClient(AnalysisRequest request)
    {
        return new MockGitClient();
    }
}
