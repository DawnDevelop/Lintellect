using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.cli.Services.Git;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.cli.Services;

internal class LanguageAnalysisOrchestrator(EProgrammingLanguage language)
{

    public async Task<AnalysisResult> RunAsync(string path)
    {
        Console.WriteLine($"Initializing {language} analyzer...");
        
        Console.WriteLine($"Analyzing solution at: {path}");
        var result = await codeAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);
        
        Console.WriteLine("Extracting Git information...");
        var gitInfo = GitInfoExtractorFactory.Create().ExtractInfo();
        
        if(gitInfo is null)
        {
            Console.WriteLine("Warning: Unable to extract Git information. Running in local/standalone mode.");
            return result;
        }

        Console.WriteLine($"Git Info Extracted:");
        Console.WriteLine($"  Pull Request: {gitInfo.Identifier}");
        Console.WriteLine($"  Commit: {gitInfo.CommitId}");
        Console.WriteLine($"  Repository: {gitInfo.RepositoryName}");

        result.GitInfo = gitInfo;
        return result;
    }

    private readonly ICodeAnalyzer codeAnalyzer = language switch
    {
        EProgrammingLanguage.CSharp => new Analyzers.Csharp.CSharpAnalyzer(),
        _ => throw new NotSupportedException($"No analyzer for {language}")
    };
}
