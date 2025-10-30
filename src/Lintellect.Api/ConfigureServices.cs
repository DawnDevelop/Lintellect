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
using Lintellect.Api.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lintellect.Api;

public static class ConfigureServices
{
    public static IServiceCollection AddGitClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind single-tenant git credentials (optional)
        services.Configure<GitCredentialsOptions>(configuration.GetSection("GitCredentials"));

        // Resolver that prefers request overrides, falls back to configured defaults
        services.AddScoped<ICredentialResolver, CredentialResolver>();

        // Register the factory for creating Git clients with dynamic credentials
        services.AddScoped<IGitClientFactory, GitClientFactory>();

        // Register the diff service
        services.AddScoped<PullRequestService>();

        return services;
    }

    public static IServiceCollection AddAnalyzerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ClaudeAnalyzerOptions>? configureClaudeOptions = null,
        Action<SemanticAnalyzerOptions>? configureSemanticOptions = null)
    {
        // Only register Claude if configured
        var claudeApiKey = configuration.GetValue<string>("ClaudeApiKey") ??
                          configuration.GetSection("ClaudeAnalyzer:ApiKey").Value;

        if (!string.IsNullOrWhiteSpace(claudeApiKey))
        {
            services.Configure<ClaudeAnalyzerOptions>(options =>
            {
                configuration.GetSection("ClaudeAnalyzer").Bind(options);
                configureClaudeOptions?.Invoke(options);
                options.ApiKey ??= claudeApiKey;
            });

            services.AddKeyedScoped<IAnalyzerService, ClaudeAnalyzerService>(
                EAnalyzers.Claude,
                (sp, key) =>
                {
                    var options = sp.GetRequiredService<IOptions<ClaudeAnalyzerOptions>>().Value;
                    var mcpServiceResolver = sp.GetRequiredService<IMcpServiceResolver>();

                    return new ClaudeAnalyzerService(options, mcpServiceResolver);
                });
        }

        // Only register Semantic (AIFoundry) if configured
        var semanticApiKey = configuration.GetValue<string>("SemanticApiKey") ??
                            configuration.GetSection("SemanticAnalyzer:ApiKey").Value;
        var semanticEndpoint = configuration.GetValue<string>("SemanticEndpoint") ??
                              configuration.GetSection("SemanticAnalyzer:Endpoint").Value;

        if (!string.IsNullOrWhiteSpace(semanticApiKey) || !string.IsNullOrWhiteSpace(semanticEndpoint))
        {
            services.Configure<SemanticAnalyzerOptions>(options =>
            {
                configuration.GetSection("SemanticAnalyzer").Bind(options);
                configureSemanticOptions?.Invoke(options);
                options.ApiKey ??= semanticApiKey;
                options.Endpoint ??= semanticEndpoint;
            });

            services.AddKeyedScoped<IAnalyzerService, SemanticAnalyzerService>(
                EAnalyzers.AIFoundry,
                (sp, key) =>
                {
                    var options = sp.GetRequiredService<IOptions<SemanticAnalyzerOptions>>().Value;
                    var mcpResolver = sp.GetRequiredService<IMcpServiceResolver>();
                    return new SemanticAnalyzerService(options, mcpResolver);
                });
        }

        // Register the resolver that picks the right analyzer based on configuration
        services.AddScoped<IAnalyzerServiceResolver, AnalyzerServiceResolver>();
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
