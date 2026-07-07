using System.CommandLine;
using Lintellect.Cli.Services;
using Lintellect.Cli.Services.Git;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Commands;

internal class StaticAnalysisCommand : Command
{
    public StaticAnalysisCommand() : base("analyze", "Run static analysis on code")
    {
        var solution = new Option<string>("--solution")
        {
            Description = "Path to .sln or .slnx (default: current directory)",
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
            },
            Aliases = { "-s" }
        };

        var serviceUrl = new Option<string>("--api-url")
        {
            Description = "Lintellect API base URL (default: LINTELLECT_API_URL environment variable)",
            Required = false,
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("LINTELLECT_API_URL") ?? string.Empty,
            Validators =
            {
                result =>
                {
                    var value = result.GetValueOrDefault<string>();
                    if (string.IsNullOrWhiteSpace(value) || !new Uri(value).IsAbsoluteUri)
                    {
                        result.AddError("API URL must be a valid absolute URI.");
                    }
                }
            },
            Aliases = { "-u" }
        };

        var apiKey = new Option<string>("--api-key")
        {
            Description = "API key (default: LINTELLECT_API_KEY environment variable)",
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("LINTELLECT_API_KEY") ?? string.Empty,
            Required = false,
            Aliases = { "-k" }
        };

        var language = new Option<EProgrammingLanguage>("--language")
        {
            Description = "Programming language (default: csharp)",
            DefaultValueFactory = _ => EProgrammingLanguage.CSharp,
            Aliases = { "-l" }
        };

        var exclusions = new Option<string[]>("--exclude")
        {
            Description = "File/folder patterns to exclude from analysis (e.g., '**/bin/**', '**/obj/**') (default: none)",
            AllowMultipleArgumentsPerToken = true,
            Aliases = { "-e" }
        };

        var enableSummaryComment = new Option<bool>("--enable-summary-comment")
        {
            Description = "Enable summary comment generation (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-esc" }
        };

        var enableInitialComment = new Option<bool>("--enable-initial-comment")
        {
            Description = "Post a placeholder 'analysis in progress' comment immediately and edit it in place with the final results when analysis completes (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-eic" }
        };

        var enableInlineSuggestions = new Option<bool>("--enable-inline-suggestions")
        {
            Description = "Enable inline suggestions (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-eis" }
        };

        var enableDescriptionSummary = new Option<bool>("--enable-description-summary")
        {
            Description = "Enable description summary (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-eds" }
        };

        var enableCodeOwners = new Option<bool>("--enable-azure-devops-code-owners")
        {
            Description = "Enable Azure DevOps code owners integration (default: false)",
            DefaultValueFactory = _ => false,
            Aliases = { "-eac" }
        };

        var enableWorkItemContext = new Option<bool>("--enable-work-item-context")
        {
            Description = "Fetch linked work items / issues and feed an AI-condensed summary into the review prompts (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-ewi" }
        };

        // Semgrep analysis options
        var enableSemgrep = new Option<bool>("--enable-semgrep")
        {
            Description = "Enable Semgrep security and quality analysis (default: true)",
            DefaultValueFactory = _ => true,
            Aliases = { "-semgrep", "-es" }
        };

        var mcpServer = new Option<EMcpServer[]>("--mcp-server")
        {
            Description = "MCP (Model Context Protocol) server to use for additional context (default: None)",
            DefaultValueFactory = _ => [],
            Aliases = { "-mcp" },
            AllowMultipleArgumentsPerToken = true
        };


        Options.Add(solution);
        Options.Add(serviceUrl);
        Options.Add(apiKey);
        Options.Add(language);
        Options.Add(exclusions);

        Options.Add(enableSummaryComment);
        Options.Add(enableInitialComment);
        Options.Add(enableInlineSuggestions);
        Options.Add(enableDescriptionSummary);
        Options.Add(enableCodeOwners);
        Options.Add(enableWorkItemContext);

        Options.Add(enableSemgrep);

        Options.Add(mcpServer);

        SetAction(async (parseResult) =>
        {
            Console.WriteLine("Starting static analysis...");
            Console.WriteLine();

            var languageOptionResult = parseResult.GetValue(language);
            var path = parseResult.GetValue(solution)!;
            var serviceUrlValue = parseResult.GetValue(serviceUrl);
            var apiKeyValue = parseResult.GetValue(apiKey);
            var exclusionPatterns = parseResult.GetValue(exclusions) ?? [];
            var enableSemgrepValue = parseResult.GetValue(enableSemgrep);
            var mcpServerValue = parseResult.GetValue(mcpServer);

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Solution Path: {path}");
            Console.WriteLine($"  Language: {languageOptionResult}");
            Console.WriteLine($"  API URL: {serviceUrlValue}");
            Console.WriteLine($"  API Key: {(string.IsNullOrEmpty(apiKeyValue) ? "Not provided" : "***")}");
            Console.WriteLine($"  Exclusions: {(exclusionPatterns.Length > 0 ? string.Join(", ", exclusionPatterns) : "None")}");
            Console.WriteLine($"  Semgrep Analysis: {(enableSemgrepValue ? "Enabled" : "Disabled")}");
            Console.WriteLine($"  MCP Server: {mcpServerValue}");

            Console.WriteLine();

            var orchestrator = new AnalysisOrchestrator(
                languageOptionResult,
                enableSemgrepValue,
                [.. exclusionPatterns]);


            var analysisResult = await orchestrator.RunAsync(path).ConfigureAwait(false);
            analysisResult.EnableDescriptionSummary = parseResult.GetValue(enableDescriptionSummary);
            analysisResult.EnableInlineSuggestions = parseResult.GetValue(enableInlineSuggestions);
            analysisResult.EnableSummaryComment = parseResult.GetValue(enableSummaryComment);
            analysisResult.EnableInitialComment = parseResult.GetValue(enableInitialComment);
            analysisResult.FileExclusions = [.. exclusionPatterns];
            analysisResult.EnableAzureDevopsCodeOwners = parseResult.GetValue(enableCodeOwners);
            analysisResult.EnableWorkItemContext = parseResult.GetValue(enableWorkItemContext);

            if (analysisResult.EnableWorkItemContext)
            {
                analysisResult.WorkItems = [.. WorkItemReferenceExtractor.ExtractFromEnvironment()];
            }

            var mcpServers = mcpServerValue ?? [];
            analysisResult.McpServer = [.. mcpServers];

            Console.WriteLine();
            Console.WriteLine($"Analysis completed: {analysisResult.Findings.Count} finding(s) detected");
            Console.WriteLine($"  Errors: {analysisResult.Findings.Count(f => f.Severity == "Error")}");
            Console.WriteLine($"  Warnings: {analysisResult.Findings.Count(f => f.Severity == "Warning")}");
            Console.WriteLine($"  Info: {analysisResult.Findings.Count(f => f.Severity == "Info")}");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(apiKeyValue) || string.IsNullOrWhiteSpace(serviceUrlValue))
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
