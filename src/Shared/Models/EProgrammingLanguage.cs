using System.Text.Json.Serialization;

namespace devops_pr_analyzer.shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EProgrammingLanguage
{
    Unknown = 0,
    CSharp,
    Python,
    Java,
    JavaScript,
    TypeScript,
    Go,
    Ruby,
    PHP,
    Swift,
    Kotlin
}
