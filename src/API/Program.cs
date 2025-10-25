using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using devops_pr_analyzer;
using devops_pr_analyzer.Apis;
using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.Apis.Infrastructure;
using devops_pr_analyzer.Apis.Options;
using devops_pr_analyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServiceDefaults;
using System.Text.Json;
using System.Text.Json.Serialization;

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
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.AddNpgsqlDbContext<ApplicationDbContext>("postgresdb");
builder.Services.AddInfrastructureServices();

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
app.MapAnalysisApi();

await app.RunAsync();
