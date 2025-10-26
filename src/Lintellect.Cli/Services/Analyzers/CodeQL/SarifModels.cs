namespace Lintellect.Cli.Services.Analyzers.CodeQL;

internal class CodeQLResult
{
    public string RuleId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public string QueryName { get; set; } = string.Empty;
}

// SARIF result models
internal class SarifResult
{
    public List<SarifRun>? Runs { get; set; }
}

internal class SarifRun
{
    public List<SarifResultItem>? Results { get; set; }
    public List<SarifInvocation>? Invocations { get; set; }
}

internal class SarifInvocation
{
    public List<SarifNotification>? ToolExecutionNotifications { get; set; }
}

internal class SarifNotification
{
    public SarifMessage? Message { get; set; }
    public string? Level { get; set; }
    public List<SarifLocation>? Locations { get; set; }
    public SarifDescriptor? Descriptor { get; set; }
}

internal class SarifDescriptor
{
    public string? Id { get; set; }
}

internal class SarifResultItem
{
    public string? RuleId { get; set; }
    public SarifMessage? Message { get; set; }
    public string? Level { get; set; }
    public List<SarifLocation>? Locations { get; set; }
}

internal class SarifMessage
{
    public string? Text { get; set; }
}

internal class SarifLocation
{
    public SarifPhysicalLocation? PhysicalLocation { get; set; }
}

internal class SarifPhysicalLocation
{
    public SarifArtifactLocation? ArtifactLocation { get; set; }
    public SarifRegion? Region { get; set; }
}

internal class SarifArtifactLocation
{
    public string? Uri { get; set; }
}

internal class SarifRegion
{
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}
