var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("docker-env");

// Add PostgreSQL server
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add database
var postgresDb = postgres.AddDatabase("postgresdb");

// Add API project with database reference
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddProject<Projects.Lintellect_Api>("API")
    .WithReference(postgresDb)
    .WaitFor(postgres)
    .WithComputeEnvironment(compose);
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

await builder.Build().RunAsync();
