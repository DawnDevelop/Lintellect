using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using devops_pr_analyzer.Application.Common.Behaviors;
using devops_pr_analyzer.Application.Common.Interfaces;
using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Application.Models;
using devops_pr_analyzer.Infrastructure.Persistence;
using devops_pr_analyzer.Infrastructure.Services;
using devops_pr_analyzer.Infrastructure.Services.AI;
using devops_pr_analyzer.Infrastructure.Services.Git;
using devops_pr_analyzer.shared.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;

namespace devops_pr_analyzer;

public static class ConfigureServices
{
    public static IServiceCollection AddGitClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Only register Azure DevOps if configured
        var devopsPat = configuration.GetValue<string>("DevopsPAT");
        var orgUrl = configuration.GetValue<string>("AzureDevOpsOrgUrl");

        if (!string.IsNullOrWhiteSpace(devopsPat) && !string.IsNullOrWhiteSpace(orgUrl))
        {
            services.AddKeyedScoped<IGitClient, AzureDevopsClientService>(
                EGitProvider.AzureDevops,
                (sp, key) =>
                {
                    var logger = sp.GetRequiredService<ILogger<AzureDevopsClientService>>();
                    return new AzureDevopsClientService(devopsPat, new Uri(orgUrl));
                });
        }

        // Only register GitHub if configured
        var githubToken = configuration.GetValue<string>("GitHubToken");
        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            services.AddKeyedScoped<IGitClient, GitHubClientService>(
                EGitProvider.GitHub,
                (sp, key) =>
                {
                    var logger = sp.GetRequiredService<ILogger<GitHubClientService>>();
                    return new GitHubClientService(githubToken, logger);
                });
        }

        // Register the resolver that picks the right client based on AnalysisResult
        services.AddScoped<IGitClientResolver, GitClientResolver>();

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
                    return new ClaudeAnalyzerService(options);
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
                    return new SemanticAnalyzerService(options);
                });
        }

        // Register the resolver that picks the right analyzer based on configuration
        services.AddScoped<IAnalyzerServiceResolver, AnalyzerServiceResolver>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register DbContext interface (DbContext is registered by Aspire)
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register background services
        services.AddSingleton<AnalysisJobQueue>();
        services.AddHostedService<AnalysisBackgroundService>();

        return services;
    }
}
