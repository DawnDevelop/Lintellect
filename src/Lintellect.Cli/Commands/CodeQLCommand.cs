using System.CommandLine;
using Lintellect.Cli.Extensions;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Commands;

internal class CodeQLCommand : Command
{


    public CodeQLCommand() : base("codeql", "Run CodeQL security and quality analysis")
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

        var timeout = new Option<int>("--timeout")
        {
            Description = "Analysis timeout in minutes",
            DefaultValueFactory = _ => 30
        };

        var output = new Option<string>("--output")
        {
            Description = "Output file path for results (optional)",
            Required = false
        };

        var format = new Option<string>("--format")
        {
            Description = "Output format: 'json', 'sarif', or 'console'",
            DefaultValueFactory = _ => "console"
        };

        var verbose = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output",
            DefaultValueFactory = _ => false
        };

        var githubToken = new Option<string>("--github-token")
        {
            Description = "GitHub Personal Access Token (required for CodeQL analysis)",
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty,
            Required = true
        };

        Options.Add(solution);
        Options.Add(timeout);
        Options.Add(output);
        Options.Add(format);
        Options.Add(verbose);
        Options.Add(githubToken);

        SetAction(async (parseResult) =>
        {
            var solutionPath = parseResult.GetValue(solution)!;
            var timeoutValue = parseResult.GetValue(timeout);
            var outputPath = parseResult.GetValue(output);
            var formatValue = parseResult.GetValue(format);
            var verboseValue = parseResult.GetValue(verbose);
            var githubTokenValue = parseResult.GetValue(githubToken);

            Console.WriteLine("========================================");
            Console.WriteLine("CodeQL Security & Quality Analysis");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Solution Path: {solutionPath}");
            Console.WriteLine($"  Timeout: {timeoutValue} minutes");
            Console.WriteLine($"  Output: {outputPath ?? "Console"}");
            Console.WriteLine($"  Format: {formatValue}");
            Console.WriteLine($"  Verbose: {verboseValue}");
            Console.WriteLine($"  GitHub Token: {(string.IsNullOrEmpty(githubTokenValue) ? "Not provided" : "***")}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Starting CodeQL analysis...");

                var analyzer = new CodeQLCSharpAnalyzer();
                var findings = await analyzer.AnalyzeAsync(solutionPath, githubTokenValue).ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine($"Analysis completed: {findings.Count} finding(s) detected");

                var findingsBySeverity = findings.GroupBy(f => f.Severity)
                    .OrderByDescending(g => g.Key == "Error" ? 3 : g.Key == "Warning" ? 2 : 1)
                    .ToList();

                foreach (var group in findingsBySeverity)
                {
                    Console.WriteLine($"{group.Key}s: {group.Count()}");
                }
                Console.WriteLine();

                // Output results based on format
                switch (formatValue!.ToLowerInvariant())
                {
                    case "json":
                        await OutputJsonResults(findings, outputPath, verboseValue).ConfigureAwait(false);
                        break;
                    case "sarif":
                        await OutputSarifResults(findings, outputPath, verboseValue).ConfigureAwait(false);
                        break;
                    case "console":
                    default:
                        OutputConsoleResults(findings, verboseValue);
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("CodeQL analysis completed successfully");
                Console.WriteLine("========================================");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CodeQL analysis failed: {ex.Message}");
                if (verboseValue)
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                throw;
            }
        });
    }

    private static void OutputConsoleResults(List<AnalyzerFindings> findings, bool verbose)
    {
        Console.WriteLine("CodeQL Analysis Results:");
        Console.WriteLine("========================");

        if (findings.Count == 0)
        {
            Console.WriteLine("✅ No issues found!");
            return;
        }

        var findingsBySeverity = findings.GroupBy(f => f.Severity)
            .OrderByDescending(g => g.Key == "Error" ? 3 : g.Key == "Warning" ? 2 : 1)
            .ToList();

        foreach (var group in findingsBySeverity)
        {
            var severityIcon = group.Key switch
            {
                "Error" => "🚨",
                "Warning" => "⚠️",
                "Info" => "ℹ️",
                _ => "📝"
            };

            Console.WriteLine();
            Console.WriteLine($"{severityIcon} {group.Key}s ({group.Count()})");
            Console.WriteLine(new string('-', 20));

            foreach (var finding in group.OrderBy(f => f.FilePath).ThenBy(f => f.Line))
            {
                Console.WriteLine($"  {finding.RuleId}: {finding.Message}");
                Console.WriteLine($"    📁 {finding.FilePath}:{finding.Line}");

                if (verbose)
                {
                    Console.WriteLine($"    🔍 Rule: {finding.RuleId}");
                    Console.WriteLine($"    📊 Severity: {finding.Severity}");
                }
                Console.WriteLine();
            }
        }
    }

    private static async Task OutputJsonResults(List<AnalyzerFindings> findings, string? outputPath, bool verbose)
    {
        var results = new
        {
            timestamp = DateTime.UtcNow,
            totalFindings = findings.Count,
            findingsBySeverity = findings.GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            findings = findings.Select(f => new
            {
                ruleId = f.RuleId,
                message = f.Message,
                filePath = f.FilePath,
                line = f.Line,
                severity = f.Severity
            })
        };

        var json = System.Text.Json.JsonSerializer.Serialize(results, JsonOptionExtensions.JsonSerializerOptions);

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(json);
        }
        else
        {
            await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
            Console.WriteLine($"Results saved to: {outputPath}");
        }
    }

    private static async Task OutputSarifResults(List<AnalyzerFindings> findings, string? outputPath, bool verbose)
    {
        // Convert to SARIF format
        var sarif = new
        {
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Lintellect CodeQL",
                            version = "1.0.0",
                            informationUri = "https://github.com/your-org/lintellect"
                        }
                    },
                    results = findings.Select(f => new
                    {
                        ruleId = f.RuleId,
                        level = f.Severity.ToLowerInvariant(),
                        message = new { text = f.Message },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new { uri = f.FilePath },
                                    region = new
                                    {
                                        startLine = f.Line,
                                        startColumn = 1,
                                        endLine = f.Line,
                                        endColumn = 1
                                    }
                                }
                            }
                        }
                    }).ToArray()
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(sarif, JsonOptionExtensions.JsonSerializerOptions);

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(json);
        }
        else
        {
            await File.WriteAllTextAsync(outputPath, json).ConfigureAwait(false);
            Console.WriteLine($"SARIF results saved to: {outputPath}");
        }
    }
}
