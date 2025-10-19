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
            Required = true,
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
            Required = true
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
            var languageOptionResult = parseResult.GetValue(language);
            var orchestrator = new LanguageAnalysisOrchestrator(languageOptionResult);
            var path = parseResult.GetValue(solution)!;
            var serviceUrlValue = parseResult.GetValue(serviceUrl);
            var apiKeyValue = parseResult.GetValue(apiKey);

            var analysisResult = await orchestrator.RunAsync(path).ConfigureAwait(false);

            if (apiKeyValue is null || serviceUrlValue is null)
            {
                foreach (var finding in analysisResult.Findings)
                {
                    Console.WriteLine($"[{finding.Severity}] {finding.RuleId}: {finding.Message} at {finding.FilePath}:{finding.Line}");
                }
            }
            else
            {
                using var client = new AnalyzerApiClientService(new Uri(serviceUrlValue), apiKeyValue);
                await client.PostAnalysisResultAsync(analysisResult).ConfigureAwait(false);
            }
                
            



            return 0;
        });
    }
}
