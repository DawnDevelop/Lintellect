using System.Text.Json.Serialization;

namespace Lintellect.Shared.Models;

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
