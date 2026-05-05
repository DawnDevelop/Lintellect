using Lintellect.Cli.Services.Git;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests;

[TestFixture]
public class WorkItemReferenceExtractorTests
{
    [TestCase("Closes #42", new[] { 42 })]
    [TestCase("closes #42", new[] { 42 })]
    [TestCase("Fixes #1 and resolves #2", new[] { 1, 2 })]
    [TestCase("This PR fixed #99.", new[] { 99 })]
    [TestCase("resolved #7", new[] { 7 })]
    [TestCase("Closes #1\nFixes #1", new[] { 1 })] // dedup
    public void ParseLinkedIssueIds_ReturnsExpected(string body, int[] expected)
    {
        var result = WorkItemReferenceExtractor.ParseLinkedIssueIds(body).ToArray();
        result.ShouldBe(expected);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("Just talking about #1234 without a keyword")]
    [TestCase("see issue 42")]
    public void ParseLinkedIssueIds_WithNoMatch_ReturnsEmpty(string? body)
    {
        WorkItemReferenceExtractor.ParseLinkedIssueIds(body).ShouldBeEmpty();
    }

    [Test]
    public void ExtractFromEnvironment_WithoutEventPath_ReturnsEmpty()
    {
        using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
        {
            ["GITHUB_EVENT_PATH"] = null
        });

        WorkItemReferenceExtractor.ExtractFromEnvironment().ShouldBeEmpty();
    }

    [Test]
    public void ExtractFromEnvironment_WithGitHubEventPayload_YieldsParsedIds()
    {
        var payload = """
            {
              "pull_request": {
                "body": "This PR closes #11 and fixes #22."
              }
            }
            """;
        var path = Path.Combine(Path.GetTempPath(), $"lintellect-event-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, payload);

        try
        {
            using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
            {
                ["GITHUB_EVENT_PATH"] = path
            });

            var result = WorkItemReferenceExtractor.ExtractFromEnvironment().Select(r => r.Id).ToArray();
            result.ShouldBe(new[] { "11", "22" });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ExtractFromEnvironment_WithMissingPullRequest_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lintellect-event-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{}");

        try
        {
            using var env = TestHelpers.SetEnvironmentVariables(new Dictionary<string, string?>
            {
                ["GITHUB_EVENT_PATH"] = path
            });

            WorkItemReferenceExtractor.ExtractFromEnvironment().ShouldBeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
