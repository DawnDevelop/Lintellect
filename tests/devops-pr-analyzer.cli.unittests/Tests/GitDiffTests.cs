using devops_pr_analyzer.cli.Extensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace devops_pr_analyzer.cli.integrationtests.Tests;

[TestFixture]
public class GitDiffTests
{
    private string _repoDir = null!;

    [SetUp]
    public void SetUp()
    {
        var asmDir = Path.GetDirectoryName(typeof(GitDiffTests).Assembly.Location)!;

        _repoDir = Path.Combine(asmDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_repoDir);
        Run("git init");
        File.WriteAllText(Path.Combine(_repoDir, "a.c"), "int main(){}");
        Run("git add .");
        Run("git commit -m init");
    }

    [TearDown]
    public void TearDown()
    {
        if (!Directory.Exists(_repoDir))
            return;

        foreach (var file in Directory.EnumerateFiles(_repoDir, "*", SearchOption.AllDirectories))
        {
            var attr = File.GetAttributes(file);
            if ((attr & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
        }

        Directory.Delete(_repoDir, recursive: true);
    }

    private void Run(string cmd)
    {
        var parts = cmd.Split(' ', 2);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = parts[0],
            Arguments = parts.Length > 1 ? parts[1] : "",
            WorkingDirectory = _repoDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi);
        p!.WaitForExit();
    }

    [Test]
    public void RunGitDiff_ShouldReturnModifiedFilesBetweenTwoCommits()
    {
        // Arrange: create and commit initial file
        var fileA = Path.Combine(_repoDir, "a.cs");
        File.WriteAllText(fileA, "Console.WriteLine(\"v1\");");
        Run("git add a.cs");
        Run("git commit -m initial");

        // Modify the existing file and add a new file
        File.WriteAllText(fileA, "Console.WriteLine(\"v2\");");
        var fileB = Path.Combine(_repoDir, "b.cs");
        File.WriteAllText(fileB, "Console.WriteLine(\"new\");");

        Run("git add .");
        Run("git commit -m update");

        // Act: compare last two commits
        var output = ChangeDetectorExtensions.RunGitDiff(_repoDir, "HEAD~1", "HEAD", new[] { "*.cs" });

        // Assert
        output.Should().Contain("a.cs");
        output.Should().Contain("b.cs");
    }

}
