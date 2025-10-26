using Lintellect.Shared.Models;

namespace Lintellect.Cli.Extensions;

internal static class LanguageMapper
{
    public static EProgrammingLanguage FromFileName(string file)
    {
        return string.IsNullOrWhiteSpace(file)
            ? EProgrammingLanguage.Unknown
            : Path.GetExtension(file).ToLowerInvariant() switch
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
