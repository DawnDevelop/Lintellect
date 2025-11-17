using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

var compose = builder.AddDockerComposeEnvironment("docker-env");

// Add PostgreSQL server
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent);

// Add database
var postgresDb = postgres.AddDatabase("postgresdb");

var apiKey = "my-secret-api";

// Add API project with database reference
builder.AddProject<Projects.Lintellect_Api>("API")
    .WithExternalHttpEndpoints()
    .WithEnvironment("LINTELLECT_API_KEY", apiKey)
    .WithEnvironment("AZURE_DEVOPS_PAT", builder.Configuration.GetValue<string>("AZURE_DEVOPS_PAT"))
    .WithEnvironment("AZURE_DEVOPS_ORG_URL", builder.Configuration.GetValue<string>("AZURE_DEVOPS_ORG_URL"))
    .WithEnvironment("SEMANTIC_API_KEY", builder.Configuration.GetValue<string>("SEMANTIC_API_KEY"))
    .WithEnvironment("SEMANTIC_ENDPOINT", builder.Configuration.GetValue<string>("SEMANTIC_ENDPOINT"))
    .WithReference(postgresDb)
    .WaitFor(postgres)
    .WithComputeEnvironment(compose);

await builder.Build().RunAsync();

