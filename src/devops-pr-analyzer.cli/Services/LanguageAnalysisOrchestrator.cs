using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.cli.Services.Git;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.cli.Services;

internal class LanguageAnalysisOrchestrator(EProgrammingLanguage language)
{

    public async Task<AnalysisResult> RunAsync(string path)
    {
        
        var result = await codeAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);
        var gitInfo = GitInfoExtractorFactory.Create().ExtractInfo();
        
        if(gitInfo is null)
        {
            Console.WriteLine("Warning: Unable to extract Git information.");
            return result;
        }

        result.GitInfo = gitInfo;
        return result;
    }

    private readonly ICodeAnalyzer codeAnalyzer = language switch
    {
        EProgrammingLanguage.CSharp => new Analyzers.Csharp.CSharpAnalyzer(),
        _ => throw new NotSupportedException($"No analyzer for {language}")
    };
}
