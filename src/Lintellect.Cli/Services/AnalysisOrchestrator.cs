using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services;

internal class AnalysisOrchestrator(
    EProgrammingLanguage language,
    bool enableCodeQL = false,
    string? githubToken = null,
    List<string>? exclusionPatterns = null)
{

    public async Task<AnalysisRequest> RunAsync(string path)
    {
        Console.WriteLine($"Initializing multi-analyzer for {language}...");

        var allFindings = new List<AnalyzerFindings>();

        // Run CodeQL analysis if enabled
        if (enableCodeQL)
        {
            Console.WriteLine("Running CodeQL security and quality analysis...");
            try
            {
                var codeQLAnalyzer = CreateCodeQLAnalyzer();
                if (codeQLAnalyzer is not null)
                {
                    var codeQLFindings = await codeQLAnalyzer.AnalyzeAsync(path, exclusionPatterns, githubToken).ConfigureAwait(false);
                    allFindings.AddRange(codeQLFindings);
                    Console.WriteLine($"✓ CodeQL analysis completed: {codeQLFindings.Count} finding(s)");
                }
                else
                {
                    Console.WriteLine($"CodeQL analysis not supported for {language}");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("CodeQL is required"))
            {
                Console.WriteLine($"❌ CodeQL analysis failed: {ex.Message}");
                Console.WriteLine("Continuing with other analyzers...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ CodeQL analysis failed: {ex.Message}");
                Console.WriteLine("Continuing with other analyzers...");
                throw;
            }
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

    private CodeQLAnalyzerBase? CreateCodeQLAnalyzer()
    {
        return language switch
        {
            EProgrammingLanguage.CSharp => new CodeQLCSharpAnalyzer(),
            EProgrammingLanguage.Python => new CodeQLPythonAnalyzer(),
            EProgrammingLanguage.Java => new CodeQLJavaAnalyzer(),
            EProgrammingLanguage.JavaScript => new CodeQLJavaScriptAnalyzer(),
            EProgrammingLanguage.TypeScript => new CodeQLTypeScriptAnalyzer(),
            EProgrammingLanguage.Go => new CodeQLGoAnalyzer(),
            EProgrammingLanguage.Ruby => new CodeQLRubyAnalyzer(),
            EProgrammingLanguage.PHP => new CodeQLPhpAnalyzer(),
            EProgrammingLanguage.Swift => new CodeQLSwiftAnalyzer(),
            EProgrammingLanguage.Kotlin => new CodeQLKotlinAnalyzer(),
            _ => null
        };
    }
}
