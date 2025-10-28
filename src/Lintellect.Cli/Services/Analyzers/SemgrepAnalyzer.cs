using System.Diagnostics;
using System.Text.Json;
using Lintellect.Cli.Interfaces;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services.Analyzers;


[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need to catch all exceptions for robust error handling in installation processes")]
internal class SemgrepAnalyzer(EProgrammingLanguage language) : ICodeAnalyzer
{
    public EProgrammingLanguage Language { get; } = language;

    public async Task<List<AnalyzerFindings>> AnalyzeAsync(string solutionPath)
    {
        try
        {
            Console.WriteLine($"Running Semgrep security analysis for {Language}...");

            // Check if Semgrep is installed
            if (!await IsDockerInstalledAsync().ConfigureAwait(false))
            {
                Console.WriteLine("ERROR: Docker not found.");
                return [];
            }

            // Run Semgrep analysis
            var findings = await RunSemgrepAnalysisAsync(solutionPath).ConfigureAwait(false);

            // Apply exclusion patterns

            Console.WriteLine($"Semgrep analysis completed: {findings.Count} finding(s) detected");
            return findings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Semgrep analysis failed: {ex.Message}");
            return [];
        }
    }

    private static async Task<bool> IsDockerInstalledAsync()
    {
        try
        {
            // Check if Docker is available and Semgrep image exists
            var (dockerSuccess, _, _) = await ExecuteCommandAsync("docker", "--version").ConfigureAwait(false);
            return dockerSuccess;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<List<AnalyzerFindings>> RunSemgrepAnalysisAsync(string solutionPath)
    {
        // Get absolute paths for Docker volume mounting
        var absoluteSolutionPath = Path.GetFullPath(solutionPath);
        var workingDirectory = Path.GetDirectoryName(absoluteSolutionPath)!;

        // Create output file in the same directory as the source code
        var outputFileName = $"semgrep-results-{Guid.NewGuid():N}.json";
        var outputFile = Path.Combine(workingDirectory, "results", outputFileName);

        try
        {
            // Run Semgrep using Docker - mount the working directory and output to the same directory

            var arguments = $"run --rm -v \"{workingDirectory}\" semgrep/semgrep semgrep scan --config=auto --json --output=results/{outputFileName} .";
            var (success, output, error) = await ExecuteCommandAsync("docker", arguments).ConfigureAwait(false);

            if (!success)
            {
                Console.WriteLine($"Semgrep Docker execution failed: {error}");
                return [];
            }

            // Check if output file was created
            if (!File.Exists(outputFile))
            {
                Console.WriteLine("Semgrep output file not found. Analysis may have completed without findings.");
                return [];
            }

            // Parse JSON results
            var jsonContent = await File.ReadAllTextAsync(outputFile).ConfigureAwait(false);
            var semgrepResults = JsonSerializer.Deserialize<SemgrepResults>(jsonContent);

            return ParseSemgrepResults(semgrepResults);
        }
        finally
        {
            // Clean up output file
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
        }
    }

    private static List<AnalyzerFindings> ParseSemgrepResults(SemgrepResults? results)
    {
        var findings = new List<AnalyzerFindings>();

        if (results?.Results == null)
        {
            return findings;
        }

        foreach (var result in results.Results)
        {
            findings.Add(new AnalyzerFindings
            {
                RuleId = $"Semgrep-{result.CheckId}",
                Message = $"[Semgrep] {result.Message}",
                FilePath = result.Path,
                Line = result.Start?.Line ?? 0,
                Severity = MapSemgrepSeverity(result.Extra?.Severity),
            });
        }

        return findings;
    }

    private static string MapSemgrepSeverity(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "error" => "Error",
            "warning" => "Warning",
            "info" => "Info",
            _ => "Info"
        };
    }

    private static async Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(
        string command,
        string arguments,
        string? workingDirectory = null)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, "", "Failed to start process");
            }

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            return (process.ExitCode == 0, output, error);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

}

/// <summary>
/// Semgrep JSON result models
/// </summary>
internal class SemgrepResults
{
    public IReadOnlyList<SemgrepResult>? Results { get; set; }
}

internal class SemgrepResult
{
    public string CheckId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public SemgrepPosition? Start { get; set; }
    public SemgrepExtra? Extra { get; set; }
}

internal class SemgrepPosition
{
    public int Line { get; set; }
    public int Col { get; set; }
}

internal class SemgrepExtra
{
    public string? Severity { get; set; }
}
