namespace Lintellect.Api.FunctionalTests.Mocks.Git;

/// <summary>
/// Mock implementation of IGitClientFactory for testing.
/// Returns a shared client instance so tests can adjust its behavior (e.g. the source head commit).
/// </summary>
public sealed class MockGitClientFactory : IGitClientFactory
{
    public static MockGitClient SharedClient { get; } = new();

    public IGitClient CreateClient(AnalysisRequest request)
    {
        return SharedClient;
    }
}
