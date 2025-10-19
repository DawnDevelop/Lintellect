using devops_pr_analyzer;
using devops_pr_analyzer.Apis;
using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.Apis.Options;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

// Register API Key configuration
builder.Services.Configure<AuthorizationOptions>(x =>
{
    x.ApiKey = builder.Configuration.GetValue<string>("ApiKey") 
        ?? throw new InvalidOperationException("API Key configuration is missing.");
});

// Register the endpoint filter
builder.Services.AddSingleton<ApiKeyEndpointFilter>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapAnalysisApi();

try
{
    await app.RunAsync();
}
catch (Exception)
{
	throw;
}
