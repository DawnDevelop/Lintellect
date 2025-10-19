using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.Extensions;

internal static class ChangeDetectorExtensions
{
    internal static string RunGitDiff(
        string cwd, 
        string target, 
        string source,
        IEnumerable<string>? includePatterns = null)
    {
        includePatterns ??= ["*"];

        // Build argument list in the correct order
        var args = new List<string>
        {
            "diff",
            "--name-only",
            target,
            source,
            "--"
        };

        args.AddRange(includePatterns);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi);
        var output = p!.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return output;
    }

    internal static IReadOnlySet<string> ToSet(string root, string diff)
        => diff.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                   .Select(f => Path.GetFullPath(Path.Combine(root, f)))
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
