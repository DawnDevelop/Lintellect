namespace devops_pr_analyzer.Application.Models;

/// <summary>
/// Model representing a CODEOWNERS file structure and ownership mappings.
/// </summary>
public class CodeOwnerModel
{
  public string Schema { get; set; } = "github_codeowners_v1";
  public string Repository { get; set; } = string.Empty;
  public DateTime GeneratedAt { get; set; }
  public List<CodeOwnerEntry> Entries { get; set; } = [];
}

/// <summary>
/// Represents a single CODEOWNERS entry with pattern and owners.
/// </summary>
public class CodeOwnerEntry
{
  public string Pattern { get; set; } = string.Empty;
  public List<string> Owners { get; set; } = [];
  public int LineNumber { get; set; }
}
