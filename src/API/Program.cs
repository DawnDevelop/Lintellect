using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using devops_pr_analyzer;
using devops_pr_analyzer.Apis;
using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.Apis.Infrastructure;
using devops_pr_analyzer.Apis.Options;
using devops_pr_analyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using ServiceDefaults;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Logging.ClearProviders();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddLogging(x => x.AddConsole());

// Register API Key configuration
builder.Services.Configure<AuthorizationOptions>(x =>
{
    x.ApiKey = builder.Configuration.GetValue<string>("ApiKey")
        ?? throw new InvalidOperationException("API Key configuration is missing.");
});

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

await app.RunAsync();
