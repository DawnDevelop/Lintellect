var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.devops_pr_analyzer_api>("devops-pr-analyzer");

await builder.Build().RunAsync();
