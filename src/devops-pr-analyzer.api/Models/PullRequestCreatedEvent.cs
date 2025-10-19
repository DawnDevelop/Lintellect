using Microsoft.Azure.Pipelines.WebApi;

namespace devops_pr_analyzer.Models;
public class PullRequestCreatedEvent
{
    public string? Id { get; set; }
    public Resource? Resource { get; set; }
    public string? EventType { get; set; }
}

public class Resource
{
    public Repository? Repository { get; set; }
    public int PullRequestId { get; set; }
}
