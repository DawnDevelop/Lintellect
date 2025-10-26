using Lintellect.Shared.Models;
using System.Text.Json;

namespace Lintellect.Cli.Extensions;

internal static class SarifParser
{
    public static IReadOnlyCollection<AnalyzerFindings> Parse(string text)
    {
        using var doc = JsonDocument.Parse(text);
        var list = new List<AnalyzerFindings>();

        foreach (var run in doc.RootElement.GetProperty("runs").EnumerateArray())
        {
            foreach (var result in run.GetProperty("results").EnumerateArray())
            {
                var ruleId = result.GetProperty("ruleId").GetString() ?? "";
                var message = result.GetProperty("message").GetProperty("text").GetString() ?? "";
                var location = result.GetProperty("locations")[0];
                var file = location.GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString() ?? "";
                var line = location.GetProperty("physicalLocation").GetProperty("region").GetProperty("startLine").GetInt32();

                list.Add(new AnalyzerFindings { RuleId = ruleId, Message = message, FilePath = file, Line = line });
            }
        }

        return list;
    }
}
