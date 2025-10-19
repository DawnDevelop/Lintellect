using devops_pr_analyzer.cli.Interfaces;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.cli.Services;

internal class LanguageAnalysisOrchestrator(EProgrammingLanguage language)
{

    public async Task<AnalysisResult> RunAsync(string path)
    {
        
        var result = await codeAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);
        return result;
    }

    private readonly ICodeAnalyzer codeAnalyzer = language switch
    {
        EProgrammingLanguage.CSharp => new Analyzers.Csharp.CSharpAnalyzer(),
        _ => throw new NotSupportedException($"No analyzer for {language}")
    };
}
