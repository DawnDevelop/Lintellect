using Lintellect.Cli.Extensions;
using Lintellect.Shared.Models;

namespace Lintellect.Cli.Services.Analyzers.CodeQL;

internal class CodeQLJavaAnalyzer : CodeQLAnalyzerBase
{
  public override EProgrammingLanguage Language => EProgrammingLanguage.Java;

  protected override string GetLanguageIdentifier()
  {
    return "java";
  }

  protected override string GetSkipDirectories()
  {
    return "target,build,.gradle,out,*.class,node_modules,coverage";
  }

  protected override async Task<string> GenerateCodeQLDatabaseAsync(string solutionPath)
  {
    TryValidateDatabasePath(solutionPath, out var solutionDir, out var databasePath);

    Console.WriteLine("Generating CodeQL database...");

    var skipDirs = GetSkipDirectories();

    var (success, output, error) = await CodeQLExtensions.ExecuteGitHubCliCommandAsync(
        $"codeql database create {databasePath} --language={GetLanguageIdentifier()} --source-root={solutionDir} --extractor-option=java.skipDirs={skipDirs}", "CodeQL Database").ConfigureAwait(false);

    if (!success)
    {
      throw new InvalidOperationException($"CodeQL database creation failed: {error}");
    }

    Console.WriteLine("✓ CodeQL database generated successfully");
    return databasePath;
  }
}
