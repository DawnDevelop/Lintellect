using System.Text.Json.Serialization;
using Lintellect.Api;
using Lintellect.Api.Apis;
using Lintellect.Api.Apis.Authorization;
using Lintellect.Api.Apis.Infrastructure;
using Lintellect.Api.Apis.Options;
using Lintellect.Api.Infrastructure.Persistence;
using Lintellect.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Logging.ClearProviders();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddLogging(x => x.AddConsole());

// Register API Key configuration
builder.Services.Configure<AuthorizationOptions>(x => x.ApiKey = builder.Configuration.GetValue<string>("ApiKey")
        ?? throw new InvalidOperationException("API Key configuration is missing."));

// Register the endpoint filter
builder.Services.AddSingleton<ApiKeyEndpointFilter>();

builder.Services.AddAnalyzerServices(builder.Configuration);

builder.Services.AddGitClients(builder.Configuration);
builder.Services.AddResiliencePolicies();
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<ApplicationDbContext>("postgresdb");
builder.Services.AddInfrastructureServices();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("postgresdb")!);
//TODO Add more hc?

var app = builder.Build();

app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.MigrateAsync();

app.UseExceptionHandler(options => { });
app.UseHttpsRedirection();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("external")
});

app.MapAnalysisApi();
app.MapAzureDevopsWebhooksApi();

await app.RunAsync();

// Make Program class accessible to tests
public partial class Program { }
