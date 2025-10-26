using Lintellect.Cli.Interfaces;
using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services;

internal class LanguageAnalysisOrchestrator(EProgrammingLanguage language)
{

    public async Task<AnalysisRequest> RunAsync(string path)
    {
        Console.WriteLine($"Initializing {language} analyzer...");

        if (codeAnalyzer is null)
        {
            throw new NotSupportedException($"No analyzer available for {language}");
        }

        Console.WriteLine($"Analyzing solution at: {path}");

        var analyzerFindings = await codeAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);

        Console.WriteLine("Extracting Git information...");
        var gitInfo = GitInfoExtractorFactory.Create().ExtractInfo();

        if (gitInfo is null)
        {
            Console.WriteLine("Warning: Unable to extract Git information. Running in local/standalone mode.");
            return new();
        }

        Console.WriteLine($"Git Info Extracted:");
        Console.WriteLine($"  Pull Request: {gitInfo.PullRequestId}");
        Console.WriteLine($"  Commit: {gitInfo.CommitId}");
        Console.WriteLine($"  Repository: {gitInfo.RepositoryName}");

        return new AnalysisRequest()
        {
            GitInfo = gitInfo,
            Language = language,
            Findings = analyzerFindings
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Reserved for later")]
    private readonly ICodeAnalyzer? codeAnalyzer = language switch
    {
        EProgrammingLanguage.CSharp => new Analyzers.Csharp.CSharpAnalyzer(),
        EProgrammingLanguage.Unknown => null,
        EProgrammingLanguage.Python => null,
        EProgrammingLanguage.Java => null,
        EProgrammingLanguage.JavaScript => null,
        EProgrammingLanguage.TypeScript => null,
        EProgrammingLanguage.Go => null,
        EProgrammingLanguage.Ruby => null,
        EProgrammingLanguage.PHP => null,
        EProgrammingLanguage.Swift => null,
        EProgrammingLanguage.Kotlin => null,
        _ => throw new NotSupportedException($"No analyzer for {language}")
    };
}
