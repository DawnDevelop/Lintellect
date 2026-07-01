using Lintellect.Api.Infrastructure.Extensions;
using Shouldly;

namespace Lintellect.Api.UnitTests.Infrastructure.Extensions;

[TestFixture]
public class DiffGenerationHelperTests
{
    [Test]
    public void AnnotateWithLineNumbers_NumbersNewFileLines()
    {
        var diff = string.Join('\n',
            "@@ -1,3 +1,4 @@",
            " line1",
            "-old2",
            "+new2",
            "+added3",
            " line4");

        var annotated = AnnotateLines(diff);

        annotated["@@ -1,3 +1,4 @@"].ShouldBe("      |");
        annotated[" line1"].ShouldBe("     1|");
        annotated["-old2"].ShouldBe("      |");   // removed line has no new-file number
        annotated["+new2"].ShouldBe("     2|");
        annotated["+added3"].ShouldBe("     3|");
        annotated[" line4"].ShouldBe("     4|");
    }

    [Test]
    public void AnnotateWithLineNumbers_EmptyInput_ReturnsEmpty()
    {
        DiffGenerationHelper.AnnotateWithLineNumbers("").ShouldBe("");
    }

    [Test]
    public void AnnotateWithLineNumbers_MatchesRealDiffPlexLinePositions()
    {
        var original = "line1\nline2\nline3\n";
        var modified = "line1\nline2-changed\nline3\ninserted4\n";

        var diff = DiffGenerationHelper.GenerateUnifiedDiff(original, modified, contextLines: 3);
        // DiffPlex emits CRLF; the annotator is line-oriented, so trim the trailing \r for comparison.
        var annotated = DiffGenerationHelper.AnnotateWithLineNumbers(diff)
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .ToArray();

        // "inserted4" is the 4th line of the modified file → annotated with new-file line 4.
        var insertedLine = annotated.Single(l => l.Contains("inserted4"));
        insertedLine.ShouldBe("     4|+inserted4");

        // "line2-changed" is the 2nd line of the modified file.
        var changedLine = annotated.Single(l => l.Contains("+line2-changed"));
        changedLine.ShouldBe("     2|+line2-changed");

        // The removed "line2" carries no new-file number.
        var removedLine = annotated.Single(l => l.Contains("-line2") && !l.Contains("changed"));
        removedLine.ShouldStartWith("      |-line2");
    }

    [Test]
    public void AnnotateWithLineNumbers_ContentBeforeFirstHunk_HasBlankNumber()
    {
        var diff = string.Join('\n',
            "--- a/file.cs",
            "+++ b/file.cs",
            "@@ -5,1 +5,1 @@",
            "+added");

        var annotated = AnnotateLines(diff);

        annotated["--- a/file.cs"].ShouldBe("      |");
        annotated["+++ b/file.cs"].ShouldBe("      |");
        annotated["+added"].ShouldBe("     5|");
    }

    // Maps each original diff line to the "<number>|" prefix the annotator added.
    private static Dictionary<string, string> AnnotateLines(string diff)
    {
        return DiffGenerationHelper.AnnotateWithLineNumbers(diff)
            .Split('\n')
            .ToDictionary(
                line => line[(line.IndexOf('|') + 1)..],
                line => line[..(line.IndexOf('|') + 1)]);
    }
}
