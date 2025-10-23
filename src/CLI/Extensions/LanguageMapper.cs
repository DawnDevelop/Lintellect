using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Extensions;

internal static class LanguageMapper
{
    public static EProgrammingLanguage FromFileName(string file)
    {
        return Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".cs" => EProgrammingLanguage.CSharp,
            ".py" => EProgrammingLanguage.Python,
            ".java" => EProgrammingLanguage.Java,
            ".js" => EProgrammingLanguage.JavaScript,
            ".ts" => EProgrammingLanguage.TypeScript,
            ".go" => EProgrammingLanguage.Go,
            _ => EProgrammingLanguage.Unknown
        };
    }
}