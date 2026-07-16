using Lintellect.Cli.Interfaces;
using Lintellect.Cli.Services.Analyzers;
using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Extensions;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services;

internal class AnalysisOrchestrator(
    EProgrammingLanguage language,
    bool enableSemgrep = false,
    List<string>? exclusionPatterns = null,
    bool enableStaticAnalysis = true)
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need to catch all exceptions for robust error handling in analysis orchestration")]
    public async Task<AnalysisRequest> RunAsync(string path)
    {
        Console.WriteLine($"Initializing multi-analyzer for {language}...");

        var allFindings = new List<AnalyzerFindings>();

        // Run Semgrep analysis if enabled
        if (enableSemgrep)
        {
            Console.WriteLine("Running Semgrep security and quality analysis...");
            try
            {
                var semgrepAnalyzer = new SemgrepAnalyzer(language);
                var semgrepFindings = await semgrepAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);
                allFindings.AddRange(semgrepFindings);
                Console.WriteLine($"✓ Semgrep analysis completed: {semgrepFindings.Count} finding(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Semgrep analysis failed: {ex.Message}");
                Console.WriteLine("Continuing with other analyzers...");
            }
        }

        if (!enableStaticAnalysis)
        {
            Console.WriteLine("Static code analysis disabled. Skipping code analysis.");
        }
        else if (_codeAnalyzer is not null)
        {
            var languageSpecificFindings = await _codeAnalyzer.AnalyzeAsync(path).ConfigureAwait(false);
            allFindings.AddRange(languageSpecificFindings);
        }
        else
        {
            Console.WriteLine($"No code analyzer available for language: {language}. Skipping code analysis.");
        }

        if (exclusionPatterns != null && exclusionPatterns.Count > 0)
        {
            var filteredFindings = allFindings.Where(finding =>
                !FilePatternMatcher.ShouldExclude(finding.FilePath, exclusionPatterns)).ToList();

            var excludedCount = allFindings.Count - filteredFindings.Count;
            if (excludedCount > 0)
            {
                Console.WriteLine($"Semgrep analysis: {excludedCount} finding(s) excluded by file patterns");
            }

            allFindings = filteredFindings;
        }


        // Extract Git information
        Console.WriteLine("Extracting Git information...");
        var gitInfo = GitInfoExtractorFactory.Create().ExtractInfo();

        if (gitInfo is null)
        {
            Console.WriteLine("Warning: Unable to extract Git information. Running in local/standalone mode.");
            return new AnalysisRequest
            {
                Language = language,
                Findings = allFindings
            };
        }

        Console.WriteLine($"Git Info Extracted:");
        Console.WriteLine($"  Pull Request: {gitInfo.PullRequestId}");
        Console.WriteLine($"  Commit: {gitInfo.CommitId}");
        Console.WriteLine($"  Repository: {gitInfo.RepositoryName}");

        return new AnalysisRequest
        {
            GitInfo = gitInfo,
            Language = language,
            Findings = allFindings
        };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Reserved for later")]
    private readonly ICodeAnalyzer? _codeAnalyzer = language switch
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
