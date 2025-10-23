var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.API>("API");

await builder.Build().RunAsync();
