using Lintellect.Shared.Models;

namespace Lintellect.Cli.Extensions;

internal static class LanguageMapper
{
    public static EProgrammingLanguage FromFileName(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            return EProgrammingLanguage.Unknown;
        }

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
