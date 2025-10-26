using Lintellect.Cli.Services;
using Lintellect.Shared.Models;
using System.CommandLine;

namespace Lintellect.Cli.Commands;

internal class StaticAnalysisCommand : Command
{
    public StaticAnalysisCommand() : base("analyze", "Run static analysis on code")
    {
        var solution = new Option<string>("--solution")
        {
            Description = "Path to .sln or .slnx",
            DefaultValueFactory = _ => ".",
            Validators =
            {
                result =>
                {
                    var value = result.GetValueOrDefault<string>();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result.AddError("Solution path cannot be empty.");
                    }
                }
            }
        };

        var serviceUrl = new Option<string>("--api-url")
        {
            Description = "AiPrReview.Service base URL",
            Required = false,
            Validators =
            {
                result =>
                {
                    var value = result.GetValueOrDefault<string>();
                    if (!new Uri(value).IsAbsoluteUri)
                    {
                        result.AddError("API URL must be a valid absolute URI.");
                    }
                }
            }
        };

        var apiKey = new Option<string>("--api-key")
        {
            Description = "API key",
            Required = false
        };

        var language = new Option<EProgrammingLanguage>("--language")
        {
            Description = "Programming language",
            DefaultValueFactory = _ => EProgrammingLanguage.CSharp
        };

        var exclusions = new Option<string[]>("--exclude")
        {
            Description = "File/folder patterns to exclude from analysis (e.g., '**/bin/**', '**/obj/**')",
            AllowMultipleArgumentsPerToken = true
        };

        var enableSummaryComment = new Option<bool>("--EnableSummaryComment")
        {
            DefaultValueFactory = _ => true
        };

        var enableInlineSuggestions = new Option<bool>("--EnableInlineSuggestions")
        {
            DefaultValueFactory = _ => true
        };

        var enableDescriptionSummary = new Option<bool>("--EnableDescriptionSummary")
        {
            DefaultValueFactory = _ => true
        };

        var enableCodeOwners = new Option<bool>("--EnableAzureDevopsCodeOwners")
        {
            DefaultValueFactory = _ => false
        };

        // Git provider credentials
        var devopsPat = new Option<string>("--devops-pat")
        {
            Description = "Azure DevOps Personal Access Token",
            Required = false
        };

        var azureDevOpsOrgUrl = new Option<string>("--azure-devops-org-url")
        {
            Description = "Azure DevOps Organization URL (e.g., https://dev.azure.com/yourorg)",
            Required = false
        };

        var githubToken = new Option<string>("--github-token")
        {
            Description = "GitHub Personal Access Token",
            Required = false
        };

        Options.Add(solution);
        Options.Add(serviceUrl);
        Options.Add(apiKey);
        Options.Add(language);
        Options.Add(exclusions);

        Options.Add(enableSummaryComment);
        Options.Add(enableInlineSuggestions);
        Options.Add(enableDescriptionSummary);
        Options.Add(enableCodeOwners);

        Options.Add(devopsPat);
        Options.Add(azureDevOpsOrgUrl);
        Options.Add(githubToken);

        SetAction(async (parseResult) =>
        {
            Console.WriteLine("Starting static analysis...");
            Console.WriteLine();

            var languageOptionResult = parseResult.GetValue(language);
            var path = parseResult.GetValue(solution)!;
            var serviceUrlValue = parseResult.GetValue(serviceUrl);
            var apiKeyValue = parseResult.GetValue(apiKey);
            var exclusionPatterns = parseResult.GetValue(exclusions) ?? [];
            var devopsPatValue = parseResult.GetValue(devopsPat);
            var azureDevOpsOrgUrlValue = parseResult.GetValue(azureDevOpsOrgUrl);
            var githubTokenValue = parseResult.GetValue(githubToken);

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Solution Path: {path}");
            Console.WriteLine($"  Language: {languageOptionResult}");
            Console.WriteLine($"  API URL: {serviceUrlValue}");
            Console.WriteLine($"  API Key: {(string.IsNullOrEmpty(apiKeyValue) ? "Not provided" : "***")}");
            Console.WriteLine($"  Exclusions: {(exclusionPatterns.Length > 0 ? string.Join(", ", exclusionPatterns) : "None")}");
            Console.WriteLine($"  DevOps PAT: {(string.IsNullOrEmpty(devopsPatValue) ? "Not provided" : "***")}");
            Console.WriteLine($"  Azure DevOps Org URL: {azureDevOpsOrgUrlValue ?? "Not provided"}");
            Console.WriteLine($"  GitHub Token: {(string.IsNullOrEmpty(githubTokenValue) ? "Not provided" : "***")}");
            Console.WriteLine();

            var orchestrator = new LanguageAnalysisOrchestrator(languageOptionResult);
            var analysisResult = await orchestrator.RunAsync(path).ConfigureAwait(false);
            analysisResult.EnableDescriptionSummary = parseResult.GetValue(enableDescriptionSummary);
            analysisResult.EnableInlineSuggestions = parseResult.GetValue(enableInlineSuggestions);
            analysisResult.EnableSummaryComment = parseResult.GetValue(enableSummaryComment);
            analysisResult.FileExclusions = exclusionPatterns.ToList();
            analysisResult.EnableAzureDevopsCodeOwners = parseResult.GetValue(enableCodeOwners);

            // Set Git provider credentials
            analysisResult.DevopsPat = devopsPatValue;
            analysisResult.AzureDevOpsOrgUrl = azureDevOpsOrgUrlValue;
            analysisResult.GitHubToken = githubTokenValue;

            Console.WriteLine();
            Console.WriteLine($"Analysis completed: {analysisResult.Findings.Count} finding(s) detected");
            Console.WriteLine($"  Errors: {analysisResult.Findings.Count(f => f.Severity == "Error")}");
            Console.WriteLine($"  Warnings: {analysisResult.Findings.Count(f => f.Severity == "Warning")}");
            Console.WriteLine($"  Info: {analysisResult.Findings.Count(f => f.Severity == "Info")}");
            Console.WriteLine();

            if (apiKeyValue is null || serviceUrlValue is null)
            {
                Console.WriteLine("No API credentials provided. Outputting findings to console:");
                Console.WriteLine();
                foreach (var finding in analysisResult.Findings)
                {
                    Console.WriteLine($"[{finding.Severity}] {finding.RuleId}: {finding.Message} at {finding.FilePath}:{finding.Line}");
                }
            }
            else
            {
                Console.WriteLine("Starting AI analysis via API...");
                using var client = new AnalyzerApiClientService(new Uri(serviceUrlValue), apiKeyValue);
                await client.StartAnalysisAsync(analysisResult).ConfigureAwait(false);
                Console.WriteLine("✓ AI analysis started successfully");
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Analysis pipeline completed successfully");
            Console.WriteLine("========================================");

            return 0;
        });
    }
}
