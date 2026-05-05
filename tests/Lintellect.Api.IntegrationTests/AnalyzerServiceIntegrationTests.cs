namespace Lintellect.Api.IntegrationTests;

/// <summary>
/// End-to-end tests against the real AI providers. Each test self-skips with
/// <c>Assert.Inconclusive</c> when the required credentials are not present in
/// environment variables, so a default <c>dotnet test</c> run on a machine with
/// no creds is a no-op rather than a failure.
///
/// To run:
///   - Azure OpenAI:  set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT_NAME
///   - Anthropic:     set CLAUDE_API_KEY
///   - then:          dotnet test tests/Lintellect.Api.IntegrationTests/
///
/// These tests cost real money on every run; keep diffs and MaxTokens small.
/// </summary>
[TestFixture]
[Category("Integration")]
public sealed class AnalyzerServiceIntegrationTests
{
    private static readonly Dictionary<string, string> SmallDiff = new()
    {
        ["TestFile.cs"] = """
            @@ -1,5 +1,6 @@
             public class Greeter
             {
            +    public string Hello(string name) => "Hi " + name;
             }
            """
    };

    private static AnalyzerServiceModel SmallAnalysisModel()
    {
        var request = new AnalysisRequest
        {
            GitProvider = EGitProvider.GitHub,
            Language = EProgrammingLanguage.CSharp,
            GitInfo = new GitInfo(1, "abc1234", "TestRepo", EGitInfoType.PullRequest, "TestProject"),
            EnableSummaryComment = true,
            EnableInlineSuggestions = true,
            EnableDescriptionSummary = true,
            Findings =
            [
                new()
                {
                    RuleId = "CS8602",
                    Message = "Dereference of a possibly null reference.",
                    FilePath = "TestFile.cs",
                    Line = 3,
                    Severity = "Warning"
                }
            ]
        };
        return new AnalyzerServiceModel(request, CopilotInstructionsPrompt: string.Empty);
    }

    private static AzureOpenAIAnalyzerOptions? TryReadAzureOpenAIOptions()
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            return null;
        }

        return new AzureOpenAIAnalyzerOptions
        {
            ApiKey = apiKey,
            Endpoint = endpoint,
            DeploymentName = deployment,
            MaxTokens = 500,
            Temperature = 0,
            MaxInlineSuggestions = 3,
        };
    }

    [Test]
    public async Task AzureOpenAIAnalyzer_GenerateSummary_returns_non_empty_text()
    {
        var options = TryReadAzureOpenAIOptions();
        if (options is null)
        {
            Assert.Inconclusive("AZURE_OPENAI_API_KEY / AZURE_OPENAI_ENDPOINT / AZURE_OPENAI_DEPLOYMENT_NAME not set; skipping.");
            return;
        }

        var resolver = Substitute.For<IMcpServiceResolver>();
        var logger = Substitute.For<ILogger<AzureOpenAIAnalyzerService>>();
        var sut = new AzureOpenAIAnalyzerService(options, resolver, logger);

        var result = await sut.GenerateSummaryAsync(SmallAnalysisModel(), SmallDiff);

        result.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Exercises the structured-output path (<c>ChatResponseFormat.ForJsonSchema&lt;InlineSuggestionsResponse&gt;()</c>),
    /// which is the most fragile part of the SK → Agent Framework migration. A successful return
    /// (regardless of suggestion count) confirms the schema round-trips through the model + JSON
    /// deserialization without exceptions.
    /// </summary>
    [Test]
    public async Task AzureOpenAIAnalyzer_GenerateInlineSuggestions_deserializes_structured_output()
    {
        var options = TryReadAzureOpenAIOptions();
        if (options is null)
        {
            Assert.Inconclusive("AZURE_OPENAI_API_KEY / AZURE_OPENAI_ENDPOINT / AZURE_OPENAI_DEPLOYMENT_NAME not set; skipping.");
            return;
        }

        var resolver = Substitute.For<IMcpServiceResolver>();
        var logger = Substitute.For<ILogger<AzureOpenAIAnalyzerService>>();
        var sut = new AzureOpenAIAnalyzerService(options, resolver, logger);

        var suggestions = await sut.GenerateInlineSuggestionsAsync(SmallAnalysisModel(), SmallDiff);

        suggestions.ShouldNotBeNull();
    }

    [Test]
    public async Task ClaudeAnalyzer_GenerateSummary_returns_non_empty_text()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Inconclusive("CLAUDE_API_KEY not set; skipping.");
            return;
        }

        var options = new ClaudeAnalyzerOptions
        {
            ApiKey = apiKey,
            MaxTokens = 500,
            Temperature = 0,
            MaxInlineSuggestions = 3,
        };

        var resolver = Substitute.For<IMcpServiceResolver>();
        var logger = Substitute.For<ILogger<ClaudeAnalyzerService>>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

        var sut = new ClaudeAnalyzerService(options, resolver, httpClientFactory, logger);

        var result = await sut.GenerateSummaryAsync(SmallAnalysisModel(), SmallDiff);

        result.ShouldNotBeNullOrWhiteSpace();
    }
}
