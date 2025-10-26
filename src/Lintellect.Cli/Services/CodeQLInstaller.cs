using System.Diagnostics;
using System.Runtime.InteropServices;
using Lintellect.Cli.Extensions;

namespace Lintellect.Cli.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We need to catch all exceptions for robust error handling in installation processes")]
internal static class CodeQLInstaller
{
    public static async Task<bool> EnsureCodeQLInstalledAsync(string? githubToken = null)
    {
        try
        {
            // Validate GitHub token for pipeline usage
            if (string.IsNullOrEmpty(githubToken))
            {
                Console.WriteLine("❌ GitHub token is required for CodeQL installation in pipeline environments");
                Console.WriteLine("Please provide a GitHub token with appropriate permissions");
                return false;
            }

            // Set GitHub token as environment variable
            CodeQLExtensions.SetGitHubToken(githubToken);

            // Check if CodeQL is already installed
            if (await IsCodeQLInstalledAsync().ConfigureAwait(false))
            {
                Console.WriteLine("✓ CodeQL is already installed");
                return true;
            }

            Console.WriteLine("CodeQL not found. Installing CodeQL via GitHub CLI...");

            // Install GitHub CLI first if not available
            if (!await IsGitHubCliInstalledAsync().ConfigureAwait(false))
            {
                Console.WriteLine("GitHub CLI not found. Installing GitHub CLI...");
                if (!await InstallGitHubCliAsync().ConfigureAwait(false))
                {
                    Console.WriteLine("❌ Failed to install GitHub CLI");
                    return false;
                }

            }

            // Install CodeQL extension
            if (!await InstallCodeQLExtensionAsync().ConfigureAwait(false))
            {
                Console.WriteLine("❌ Failed to install CodeQL extension");
                return false;
            }

            CodeQLExtensions.RefreshPathAfterInstallation();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected CodeQL installation error: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> IsCodeQLInstalledAsync()
    {
        var (success, _, _) = await CodeQLExtensions.ExecuteGitHubCliCommandAsync("codeql version", "CodeQL Check Install").ConfigureAwait(false);
        return success;
    }

    private static async Task<bool> IsGitHubCliInstalledAsync()
    {
        var (success, _, _) = await CodeQLExtensions.ExecuteGitHubCliCommandAsync("version", "GitHub CLI Check").ConfigureAwait(false);
        return success;
    }

    private static async Task<bool> InstallGitHubCliAsync()
    {
        try
        {
            var platform = GetCurrentPlatform();
            var installCommand = GetGitHubCliInstallCommand(platform);

            if (string.IsNullOrEmpty(installCommand))
            {
                Console.WriteLine("❌ GitHub CLI installation not supported on this platform");
                return false;
            }

            Console.WriteLine("Installing GitHub CLI...");

            var packageManager = GetPackageManagerExecutable();
            var arguments = GetPackageManagerArguments(installCommand);

            var startInfo = new ProcessStartInfo
            {
                FileName = packageManager,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            // Read output in real-time
            var outputTask = ReadProcessOutputAsync(process.StandardOutput, "[GitHub CLI]");
            var errorTask = ReadProcessOutputAsync(process.StandardError, "[GitHub CLI Error]");

            await process.WaitForExitAsync().ConfigureAwait(false);

            // Wait for output tasks to complete
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

            return process.ExitCode is 0 or (-1978335189);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error during GitHub CLI installation: {ex.Message}");
            return false;
        }
    }

    private static async Task ReadProcessOutputAsync(StreamReader reader, string prefix)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
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
            Console.WriteLine($"   {prefix} Error reading output: {ex.Message}");
        }
    }

    private static async Task<bool> InstallCodeQLExtensionAsync()
    {
        Console.WriteLine("Installing CodeQL extension...");
        var (success, _, _) = await CodeQLExtensions.ExecuteGitHubCliCommandAsync("extensions install github/gh-codeql", "CodeQL Extension").ConfigureAwait(false);
        return success;
    }


    private static OSPlatform GetCurrentPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? OSPlatform.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? OSPlatform.OSX
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? OSPlatform.Linux
            : throw new PlatformNotSupportedException("Unsupported operating system");
    }

    private static string GetPackageManagerExecutable()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "winget"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? "brew"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "bash"
            : throw new PlatformNotSupportedException("Unsupported operating system");
    }

    private static string GetPackageManagerArguments(string command)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? command.Replace("winget ", "")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? command.Replace("brew ", "")
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? $"-c \"{command}\""
            : throw new PlatformNotSupportedException("Unsupported operating system");
    }

    private static string GetGitHubCliInstallCommand(OSPlatform platform)
    {
        return platform == OSPlatform.Windows
            ? "winget install --id GitHub.cli --silent"
            : platform == OSPlatform.OSX
            ? "brew install gh"
            : platform == OSPlatform.Linux
            ? "curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && echo \"deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main\" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null && sudo apt update && sudo apt install gh"
            : throw new PlatformNotSupportedException("Unsupported operating system");
    }
}
