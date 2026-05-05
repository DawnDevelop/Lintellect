using FluentValidation;
using Lintellect.Api.Application.Common.Behaviors;
using Lintellect.Api.Application.Common.Interfaces;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Infrastructure.Persistence;
using Lintellect.Api.Infrastructure.Resilience;
using Lintellect.Api.Infrastructure.Services.AI;
using Lintellect.Api.Infrastructure.Services.AI.MCPs;
using Lintellect.Api.Infrastructure.Services.Analysis;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Api.Infrastructure.Services.Webhooks;
using Lintellect.Api.Infrastructure.Services.WorkItems;
using Lintellect.Api.Infrastructure.Telemetry;
using Lintellect.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lintellect.Api;

public static class ConfigureServices
{
    public static IServiceCollection AddGitClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GitCredentialsOptions>(configuration.GetSection("GitCredentials"));
        services.PostConfigure<GitCredentialsOptions>(options =>
        {
            options.AzureDevOps ??= new GitCredentialsOptions.AzureDevOpsCredentials();
            options.AzureDevOps.Pat ??= configuration.GetValue<string>("AZURE_DEVOPS_PAT");
            options.AzureDevOps.OrgUrl ??= configuration.GetValue<string>("AZURE_DEVOPS_ORG_URL");

            options.GitHub ??= new GitCredentialsOptions.GitHubCredentials();
            options.GitHub.Token ??= configuration.GetValue<string>("GITHUB_TOKEN");
        });

        // Register the factory for creating Git clients with dynamic credentials
        services.AddScoped<IGitClientFactory, GitClientFactory>();

        // Register the diff service
        services.AddScoped<PullRequestService>();

        // Register work-item context services (toggled per-job via AnalysisRequest.EnableWorkItemContext)
        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<IWorkItemSummarizer, WorkItemSummarizer>();

        return services;
    }

    public static IServiceCollection AddAnalyzerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ClaudeAnalyzerOptions>? configureClaudeOptions = null,
        Action<AzureOpenAIAnalyzerOptions>? configureAzureOpenAIOptions = null)
    {
        // Only register Claude if configured
        var claudeApiKey = configuration.GetValue<string>("CLAUDE_API_KEY") ??
                          configuration.GetSection("ClaudeAnalyzer:ApiKey").Value;

        if (!string.IsNullOrWhiteSpace(claudeApiKey))
        {
            services.Configure<ClaudeAnalyzerOptions>(options =>
            {
                configuration.GetSection("ClaudeAnalyzer").Bind(options);
                configureClaudeOptions?.Invoke(options);
                options.ApiKey ??= claudeApiKey;
            });

            services.AddScoped<IAnalyzerService, ClaudeAnalyzerService>(
                (sp) =>
                {
                    var options = sp.GetRequiredService<IOptions<ClaudeAnalyzerOptions>>().Value;
                    var mcpServiceResolver = sp.GetRequiredService<IMcpServiceResolver>();
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<ClaudeAnalyzerService>>();

                    return new ClaudeAnalyzerService(options, mcpServiceResolver, httpClientFactory, logger);
                });
        }
        else
        {
            var azureOpenAIApiKey = configuration.GetValue<string>("AZURE_OPENAI_API_KEY") ??
                        configuration.GetSection("AzureOpenAIAnalyzer:ApiKey").Value;

            var azureOpenAIEndpoint = configuration.GetValue<string>("AZURE_OPENAI_ENDPOINT") ??
                                  configuration.GetSection("AzureOpenAIAnalyzer:Endpoint").Value;

            var azureOpenAIDeploymentName = configuration.GetValue<string>("AZURE_OPENAI_DEPLOYMENT_NAME") ??
                                         configuration.GetSection("AzureOpenAIAnalyzer:DeploymentName").Value;

            services.Configure<AzureOpenAIAnalyzerOptions>(options =>
            {
                configuration.GetSection("AzureOpenAIAnalyzer").Bind(options);
                configureAzureOpenAIOptions?.Invoke(options);

                options.ApiKey ??= azureOpenAIApiKey;
                options.Endpoint ??= azureOpenAIEndpoint;

                options.DeploymentName ??= azureOpenAIDeploymentName;
                options.DeploymentName ??= "gpt-4o"; //fallback
            });

            services.AddScoped<IAnalyzerService, AzureOpenAIAnalyzerService>(
                (sp) =>
                {
                    var options = sp.GetRequiredService<IOptions<AzureOpenAIAnalyzerOptions>>().Value;
                    var mcpResolver = sp.GetRequiredService<IMcpServiceResolver>();
                    var logger = sp.GetRequiredService<ILogger<AzureOpenAIAnalyzerService>>();
                    return new AzureOpenAIAnalyzerService(options, mcpResolver, logger);
                });
        }

        services.AddKeyedSingleton<IMcpService, Context7McpService>(EMcpServer.Context7);
        services.AddKeyedSingleton<IMcpService, MicrosoftDocsMcpService>(EMcpServer.MicrosoftDocs);
        services.AddScoped<IMcpServiceResolver, McpServiceResolver>();

        return services;
    }

    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        // Add HTTP client with resilience policies
        services.AddHttpClient("ClaudeApi")
            .AddPolicyHandler(ResiliencePolicies.GetAiApiPolicy());

        services.AddHttpClient("GitHubApi")
            .AddPolicyHandler(ResiliencePolicies.GetCombinedPolicy());

        services.AddHttpClient("AzureDevOpsApi")
            .AddPolicyHandler(ResiliencePolicies.GetCombinedPolicy());

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Mediator
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>), typeof(LoggingBehaviour<,>)];
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register DbContext interface (DbContext is registered by Aspire)
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register background services for analysis
        services.AddSingleton<AnalysisJobQueue>();
        services.AddHostedService<AnalysisBackgroundService>();

        // Register background services for webhooks
        services.AddSingleton<WebhookJobQueue>();
        services.AddHostedService<WebhookBackgroundService>();

        // Register metrics
        services.AddSingleton<AnalysisMetrics>();

        return services;
    }
}
