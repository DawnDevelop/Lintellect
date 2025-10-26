using System.Diagnostics;
using System.Runtime.InteropServices;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Extensions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need to catch all exceptions for robust error handling in installation processes")]
internal static class CodeQLExtensions
{


    public static string? FindGitHubCliPath()
    {
        // Try PATH first
        var pathResult = FindInPath();
        if (!string.IsNullOrEmpty(pathResult))
        {
            return pathResult;
        }

        // Fallback to common installation paths
        return GetCommonGitHubCliPaths().FirstOrDefault(File.Exists);
    }

    private static string? FindInPath()
    {
        try
        {
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = "gh",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                }
            }
        }
        catch { }
        return null;
    }
    private static string[] GetCommonGitHubCliPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return
            [
                @"C:\Program Files\GitHub CLI\gh.exe",
                @"C:\Program Files (x86)\GitHub CLI\gh.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\GitHub CLI\gh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\Programs\GitHub CLI\gh.exe")
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return
            [
                "/usr/local/bin/gh",
                "/opt/homebrew/bin/gh",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/gh")
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return
            [
                "/usr/bin/gh",
                "/usr/local/bin/gh",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/gh")
            ];
        }

        return [];
    }

    public static async Task<(bool Success, string Output, string Error)> ExecuteGitHubCliCommandAsync(string arguments, string? prefix = null)
    {
        var ghPath = FindGitHubCliPath();
        if (string.IsNullOrEmpty(ghPath))
        {
            return (false, "", "GitHub CLI not found");
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = ghPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            });

            if (process == null)
            {
                return (false, "", "Failed to start GitHub CLI process");
            }

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            var outputTask = ReadProcessOutputAsync(process.StandardOutput, outputBuilder, prefix ?? "[GitHub CLI]");
            var errorTask = ReadProcessOutputAsync(process.StandardError, errorBuilder, prefix != null ? $"[{prefix} Error]" : "[GitHub CLI Error]");

            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

            return (process.ExitCode == 0, outputBuilder.ToString(), errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    private static async Task ReadProcessOutputAsync(StreamReader reader, System.Text.StringBuilder builder, string prefix)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    builder.AppendLine(line);
                    Console.WriteLine($"   {prefix} {line}");
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Reader was disposed, which is expected when process ends
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{prefix} Error reading output: {ex.Message}");
        }
    }

    public static void SetGitHubToken(string token)
    {
        Environment.SetEnvironmentVariable("GH_TOKEN", token);
    }

    public static void RefreshPathAfterInstallation()
    {
        // Try to find gh in PATH again (this will refresh the PATH search)
        var ghPath = FindGitHubCliPath();
        if (!string.IsNullOrEmpty(ghPath))
        {
            // If found, add the directory containing gh to PATH
            var ghDirectory = Path.GetDirectoryName(ghPath);
            if (!string.IsNullOrEmpty(ghDirectory))
            {
                var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";

                if (!currentPath.Contains(ghDirectory))
                {
                    var newPath = $"{currentPath}{separator}{ghDirectory}";
                    Environment.SetEnvironmentVariable("PATH", newPath);
                }
            }
        }
    }

    public static async Task DownloadCodeQLQueryPacksAsync(EProgrammingLanguage language)
    {
        Console.WriteLine("Downloading CodeQL query packs...");

        var queryPack = $"codeql/{language.ToString().ToLowerInvariant()}-queries";

        try
        {
            var (success, output, error) = await ExecuteGitHubCliCommandAsync(
                $"codeql pack download {queryPack}", "CodeQL Pack Download").ConfigureAwait(false);

            if (!success)
            {
                Console.WriteLine($"Warning: Failed to download query pack '{queryPack}': {error}");
            }

            Console.WriteLine($"✓ Downloaded query pack: {queryPack}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Exception downloading query pack '{queryPack}': {ex.Message}");
        }
    }

}

