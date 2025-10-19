using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace devops_pr_analyzer.cli.integrationtests;

internal class TestHelpers
{
    /// <summary>
    /// Gets the absolute path to a test fixture directory
    /// </summary>
    public static string GetFixturePath(string relativePath)
    {
        var testProjectDir = GetTestProjectDirectory();
        return Path.Combine(testProjectDir, relativePath);
    }

    /// <summary>
    /// Gets the root directory of the test project
    /// </summary>
    public static string GetTestProjectDirectory()
    {
        var assembly = typeof(TestHelpers).Assembly;
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        
        // Navigate up to find the test project root (contains .csproj)
        var current = new DirectoryInfo(assemblyDir);
        while (current != null)
        {
            if (Directory.GetFiles(current.FullName, "*.csproj").Length > 0)
                return current.FullName;
            current = current.Parent;
        }
        
        throw new InvalidOperationException("Could not find test project directory");
    }

    /// <summary>
    /// Ensures that a solution file exists at the given path
    /// </summary>
    public static void EnsureSolutionExists(string solutionPath)
    {
        if (!File.Exists(solutionPath) && !solutionPath.EndsWith(".slnx"))
        {
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");
        }
        if (!File.Exists(solutionPath) && solutionPath.EndsWith(".slnx"))
        {
            // .slnx files might be XML based, verify it's readable
            if (!File.Exists(solutionPath))
                throw new FileNotFoundException($"Solution file not found: {solutionPath}");
        }
    }

    /// <summary>
    /// Extracts a sample repository zip file to the Fixtures directory in the output directory
    /// </summary>
    /// <param name="zipFileName">Name of the zip file (e.g., "SimpleRepo.zip")</param>
    internal static void UnZipSampleRepo(string zipFileName)
    {
        var assembly = typeof(TestHelpers).Assembly;
        var assemblyLocation = assembly.Location;
        var outputDir = Path.GetDirectoryName(assemblyLocation)!;
        
        var zipPath = Path.Combine(outputDir, "SampleRepos", zipFileName);
        
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"Zip file not found: {zipPath}");
        }

        var fixturesDir = Path.Combine(outputDir, "Fixtures");
        Directory.CreateDirectory(fixturesDir);

        // Extract the zip file to a temp location first
        var repoName = Path.GetFileNameWithoutExtension(zipFileName);
        var tempExtractPath = Path.Combine(fixturesDir, $"{repoName}_temp");
        var finalExtractPath = Path.Combine(fixturesDir, repoName);

        // Remove existing directories if they exist
        if (Directory.Exists(tempExtractPath))
        {
            Directory.Delete(tempExtractPath, true);
        }
        if (Directory.Exists(finalExtractPath))
        {
            Directory.Delete(finalExtractPath, true);
        }

        // Extract the zip file
        ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

        // Check if the zip contains a single root folder with the same name
        var extractedItems = Directory.GetFileSystemEntries(tempExtractPath);
        if (extractedItems.Length == 1 && Directory.Exists(extractedItems[0]))
        {
            var innerFolder = extractedItems[0];
            var innerFolderName = Path.GetFileName(innerFolder);
            
            // If the zip has a nested folder (e.g., SimpleRepo/SimpleRepo/...), move contents up
            if (innerFolderName.Equals(repoName, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(innerFolder, finalExtractPath);
                Directory.Delete(tempExtractPath, true);
            }
            else
            {
                // Otherwise, just rename the temp folder
                Directory.Move(tempExtractPath, finalExtractPath);
            }
        }
        else
        {
            // Multiple items at root, just rename the temp folder
            Directory.Move(tempExtractPath, finalExtractPath);
        }
    }
}
