using System;
using System.CommandLine;
using devops_pr_analyzer.cli.Services;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.cli.Commands;

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

        Options.Add(solution);
        Options.Add(serviceUrl);
        Options.Add(apiKey);
        Options.Add(language);

        SetAction(async (parseResult) =>
        {
            Console.WriteLine("Starting static analysis...");
            Console.WriteLine();

            var languageOptionResult = parseResult.GetValue(language);
            var path = parseResult.GetValue(solution)!;
            var serviceUrlValue = parseResult.GetValue(serviceUrl);
            var apiKeyValue = parseResult.GetValue(apiKey);

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Solution Path: {path}");
            Console.WriteLine($"  Language: {languageOptionResult}");
            Console.WriteLine($"  API URL: {serviceUrlValue}");
            Console.WriteLine($"  API Key: {(string.IsNullOrEmpty(apiKeyValue) ? "Not provided" : "***")}");
            Console.WriteLine();

            var orchestrator = new LanguageAnalysisOrchestrator(languageOptionResult);
            var analysisResult = await orchestrator.RunAsync(path).ConfigureAwait(false);

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
                Console.WriteLine("Posting results to API...");
                using var client = new AnalyzerApiClientService(new Uri(serviceUrlValue), apiKeyValue);
                await client.PostAnalysisResultAsync(analysisResult).ConfigureAwait(false);
                Console.WriteLine("✓ Results successfully posted to API");
            }
            
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Analysis pipeline completed successfully");
            Console.WriteLine("========================================");

            return 0;
        });
    }
}
