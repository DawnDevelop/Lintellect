using Lintellect.Api.Infrastructure.Services.Git;

namespace Lintellect.Api.UnitTests.Infrastructure.Services.Git;

[TestFixture]
public class CodeOwnersPathFilterTests
{
    [Test]
    public void NoMatchingRules_ReturnsEmpty()
    {
        var content = """
            *.py @python-team
            /docs/ @docs-team
            """;

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["src/Program.cs", "README.md"]);

        result.ShouldBeEmpty();
    }

    [Test]
    public void MatchingExtensionPattern_KeptInResult()
    {
        var content = """
            *.cs @csharp-team
            *.py @python-team
            """;

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["src/Program.cs"]);

        result.ShouldContain("*.cs @csharp-team");
        result.ShouldNotContain("@python-team");
    }

    [Test]
    public void MatchingDirectoryPattern_KeptInResult()
    {
        var content = """
            /docs/ @docs-team
            /src/ @backend-team
            """;

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["docs/api.md"]);

        result.ShouldContain("/docs/ @docs-team");
        result.ShouldNotContain("@backend-team");
    }

    [Test]
    public void RecursiveGlob_MatchesNestedFiles()
    {
        var content = "src/**/*.ts @ts-team";

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["src/api/handlers/users.ts"]);

        result.ShouldContain("@ts-team");
    }

    [Test]
    public void CommentsAndBlankLines_PreservedWhenAdjacentToMatch()
    {
        var content = """
            # Backend code
            *.cs @csharp-team

            # Docs
            *.md @docs-team
            """;

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["src/Program.cs"]);

        result.ShouldContain("# Backend code");
        result.ShouldContain("*.cs @csharp-team");
        result.ShouldNotContain("# Docs");
        result.ShouldNotContain("*.md");
    }

    [Test]
    public void EmptyContent_ReturnsEmpty()
    {
        CodeOwnersPathFilter.FilterMatchingLines("", ["src/Program.cs"]).ShouldBeEmpty();
        CodeOwnersPathFilter.FilterMatchingLines("   ", ["src/Program.cs"]).ShouldBeEmpty();
    }

    [Test]
    public void EmptyChangedPaths_ReturnsEmpty()
    {
        var content = "*.cs @csharp-team";
        CodeOwnersPathFilter.FilterMatchingLines(content, []).ShouldBeEmpty();
    }

    [Test]
    public void MultipleChangedPaths_KeepsRulesMatchingAny()
    {
        var content = """
            *.cs @csharp-team
            *.py @python-team
            *.md @docs-team
            """;

        var result = CodeOwnersPathFilter.FilterMatchingLines(content, ["src/Program.cs", "docs/README.md"]);

        result.ShouldContain("*.cs @csharp-team");
        result.ShouldContain("*.md @docs-team");
        result.ShouldNotContain("*.py @python-team");
    }
}
