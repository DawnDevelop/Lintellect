using System.Text.Json;
using Lintellect.Cli.Extensions;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests.CodeQL;

[TestFixture]
public class CodeQLSarifParsingTests : CodeQLTestBase
{
    [Test]
    public void DebugSarifParsing()
    {
        // Arrange
        var sarifContent = """
        {
          "runs": [
            {
              "results": [
                {
                  "ruleId": "cs/hardcoded-credential",
                  "message": {
                    "text": "Hardcoded credential detected."
                  },
                  "locations": [
                    {
                      "physicalLocation": {
                        "artifactLocation": {
                          "uri": "file://src/main/Program.cs"
                        },
                        "region": {
                          "startLine": 10
                        }
                      }
                    }
                  ],
                  "level": "error"
                }
              ]
            }
          ]
        }
        """;
        var sarifResult = JsonSerializer.Deserialize<SarifResult>(sarifContent, JsonOptionExtensions.JsonSerializerOptions);

        // Debug: Check if deserialization worked
        sarifResult.ShouldNotBeNull();
        sarifResult.Runs.ShouldNotBeNull();
        sarifResult.Runs.Count.ShouldBe(1);
        sarifResult.Runs[0].Results.ShouldNotBeNull();
        sarifResult.Runs[0].Results!.Count.ShouldBe(1);

        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", sarifResult!);

        // Debug: Check what we got
        Console.WriteLine($"Results count: {results.Count}");
        if (results.Count > 0)
        {
            Console.WriteLine($"First result RuleId: {results[0].RuleId}");
            Console.WriteLine($"First result FilePath: {results[0].FilePath}");
        }

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(1);
    }

    [Test]
    public void ParseSarifResults_WithFindings_ShouldReturnCodeQLResults()
    {
        var sarifContent = """
        {
            "runs": [
            {
                "results": [
                {
                    "ruleId": "cs/hardcoded-credential",
                    "message": {
                    "text": "Hardcoded credential detected."
                    },
                    "locations": [
                    {
                        "physicalLocation": {
                            "artifactLocation": {
                                "uri": "file://src/main/Program.cs"
                            },
                            "region": {
                                "startLine": 10
                            }
                            }
                        }
                    ],
                    "level": "error"
                },
                {
                    "ruleId": "cs/weak-cryptographic-algorithm",
                    "message": {
                    "text": "Weak cryptographic algorithm used."
                    },
                    "locations": [
                        {
                            "physicalLocation": {
                                "artifactLocation": {
                                    "uri": "file://src/utils/Crypto.cs"
                                },
                                "region": {
                                    "startLine": 25
                                }
                            }
                        }
                    ],
                    "level": "warning"
                }
                ]
            }
            ]
        }
        """;

        var sarifResult = JsonSerializer.Deserialize<SarifResult>(sarifContent, JsonOptionExtensions.JsonSerializerOptions);

        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", sarifResult!);

        // Assert
        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);

        results[0].RuleId.ShouldBe("cs/hardcoded-credential");
        results[0].FilePath.ShouldBe("src/main/Program.cs");
        results[0].Line.ShouldBe(10);
        results[0].Severity.ShouldBe("Error");
        results[0].Message.ShouldBe("Hardcoded credential detected.");

        results[1].RuleId.ShouldBe("cs/weak-cryptographic-algorithm");
        results[1].FilePath.ShouldBe("src/utils/Crypto.cs");
        results[1].Line.ShouldBe(25);
        results[1].Severity.ShouldBe("Warning");
        results[1].Message.ShouldBe("Weak cryptographic algorithm used.");
    }

    [Test]
    public void ParseSarifResults_WithEmptyResults_ShouldReturnEmptyList()
    {
        // Arrange
        var sarifContent = """
        {
                "runs": [
                {
                    "results": []
                }
            ]
        }
        """;
        var sarifResult = JsonSerializer.Deserialize<SarifResult>(sarifContent);

        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", sarifResult!);

        // Assert
        results.ShouldBeEmpty();
    }

    [Test]
    public void ParseSarifResults_WithNullRuns_ShouldReturnEmptyList()
    {
        // Arrange
        var sarifResult = new SarifResult { Runs = null };

        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", sarifResult);

        // Assert
        results.ShouldBeEmpty();
    }

    [Test]
    public void ParseSarifResults_WithNullSarifResult_ShouldReturnEmptyList()
    {
        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", (SarifResult?)null!);

        // Assert
        results.ShouldBeEmpty();
    }

    [Test]
    public void MapSarifLevelToSeverity_ShouldMapCorrectly()
    {
        // Arrange & Act & Assert
        InvokePrivateStaticMethod<string>(typeof(CodeQLAnalyzerBase), "MapSarifLevelToSeverity", "error").ShouldBe("Error");
        InvokePrivateStaticMethod<string>(typeof(CodeQLAnalyzerBase), "MapSarifLevelToSeverity", "warning").ShouldBe("Warning");
        InvokePrivateStaticMethod<string>(typeof(CodeQLAnalyzerBase), "MapSarifLevelToSeverity", "note").ShouldBe("Info");
        InvokePrivateStaticMethod<string>(typeof(CodeQLAnalyzerBase), "MapSarifLevelToSeverity", "unknown").ShouldBe("Info");
        InvokePrivateStaticMethod<string>(typeof(CodeQLAnalyzerBase), "MapSarifLevelToSeverity", (string?)null!).ShouldBe("Info");
    }

    [Test]
    public void ParseCodeQLResults_ShouldSkipEmptyFilePaths()
    {
        // Arrange
        var codeQLResults = new List<CodeQLResult>
        {
          new() { RuleId = "valid", FilePath = "/src/Program.cs", Line = 1, Severity = "Error", Message = "Valid finding" },
          new() { RuleId = "empty", FilePath = "", Line = 0, Severity = "Warning", Message = "Empty file path" },
          new() { RuleId = "null", FilePath = "", Line = 0, Severity = "Info", Message = "Null file path" }
        };

        // Act
        var findings = InvokePrivateStaticMethod<List<AnalyzerFindings>>(typeof(CodeQLAnalyzerBase), "ParseCodeQLResults", codeQLResults);

        // Assert
        findings.ShouldNotBeNull();
        findings.Count.ShouldBe(1);
        findings[0].RuleId.ShouldBe("CodeQL-valid");
    }

    [Test]
    public void ParseCodeQLResults_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var codeQLResults = new List<CodeQLResult>();

        // Act
        var findings = InvokePrivateStaticMethod<List<AnalyzerFindings>>(typeof(CodeQLAnalyzerBase), "ParseCodeQLResults", codeQLResults);

        // Assert
        findings.ShouldBeEmpty();
    }

    [Test]
    public void ParseCodeQLResults_ShouldPrefixRuleIdWithCodeQL()
    {
        // Arrange
        var codeQLResults = new List<CodeQLResult>
        {
          new() { RuleId = "cs/sql-injection", FilePath = "/src/Program.cs", Line = 1, Severity = "Error", Message = "SQL injection vulnerability" }
        };

        // Act
        var findings = InvokePrivateStaticMethod<List<AnalyzerFindings>>(typeof(CodeQLAnalyzerBase), "ParseCodeQLResults", codeQLResults);

        // Assert
        findings.ShouldNotBeNull();
        findings.Count.ShouldBe(1);
        findings[0].RuleId.ShouldBe("CodeQL-cs/sql-injection");
    }

    [Test]
    public void ParseSarifResults_WithInvalidFilePaths_ShouldHandleGracefully()
    {
        // Arrange
        var sarifContent = """
        {
          "runs": [
            {
              "results": [
                {
                  "ruleId": "cs/test-rule",
                  "message": {
                    "text": "Test message"
                  },
                  "locations": [
                    {
                      "physicalLocation": {
                        "artifactLocation": {
                          "uri": ""
                        },
                        "region": {
                          "startLine": 1
                        }
                      }
                    }
                  ],
                  "level": "error"
                }
              ]
            }
          ]
        }
        """;
        var sarifResult = JsonSerializer.Deserialize<SarifResult>(sarifContent);

        // Act
        var results = InvokePrivateStaticMethod<List<CodeQLResult>>(typeof(CodeQLAnalyzerBase), "ParseSarifResults", sarifResult!);

        // Assert
        results.ShouldBeEmpty();
    }
}
