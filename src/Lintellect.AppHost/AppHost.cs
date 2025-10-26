var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL server
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Add database
var postgresDb = postgres.AddDatabase("postgresdb");

// Add API project with database reference
builder.AddProject<Projects.Lintellect_Api>("API")
    .WithReference(postgresDb);

await builder.Build().RunAsync();
