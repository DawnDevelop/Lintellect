using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using devops_pr_analyzer.Interfaces;
using devops_pr_analyzer.Models;
using devops_pr_analyzer.Services;
using devops_pr_analyzer.Services.AI;
using devops_pr_analyzer.Services.Git;
using devops_pr_analyzer.shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace devops_pr_analyzer;

public static class ConfigureServices
{
    public static IServiceCollection AddGitClients(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddKeyedScoped<IGitClient, AzureDevopsClientService>(
            EGitProvider.AzureDevops,
            (sp, key) =>
            {
                
                var pat = configuration.GetValue<string>("DevopsPAT")
                    ?? throw new InvalidOperationException("DevopsPAT configuration is missing");
                var orgUrl = configuration.GetValue<string>("AzureDevOpsOrgUrl")
                    ?? throw new InvalidOperationException("AzureDevOpsOrgUrl configuration is missing");


                return new AzureDevopsClientService(pat, new Uri(orgUrl));
            });

        // Register GitHub client when implemented
        // services.AddKeyedScoped<IGitClient, GitHubClientService>(
        //     EGitProvider.GitHub,
        //     (sp, key) =>
        //     {
        //         var token = configuration.GetValue<string>("GitHubToken")
        //             ?? throw new InvalidOperationException("GitHubToken configuration is missing");
        //         return new GitHubClientService(token);
        //     });

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
        // Configure Claude Analyzer
        services.Configure<ClaudeAnalyzerOptions>(options =>
        {
            configuration.GetSection("ClaudeAnalyzer").Bind(options);   
            configureClaudeOptions?.Invoke(options);

            // Fallback to root level config
            options.ApiKey ??= configuration.GetValue<string>("ClaudeApiKey");
        });

        services.AddKeyedScoped<IAnalyzerService, ClaudeAnalyzerService>(
            EAnalyzers.Claude,
            (sp, key) =>
            {
                var options = sp.GetRequiredService<IOptions<ClaudeAnalyzerOptions>>().Value;
                
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    throw new InvalidOperationException(
                        "ClaudeApiKey is required. Configure it via ClaudeAnalyzer:ApiKey or ClaudeApiKey in configuration.");
                }

                return new ClaudeAnalyzerService(options);
            });

        // Configure Semantic (AIFoundry) Analyzer
        services.Configure<SemanticAnalyzerOptions>(options =>
        {
            // Bind from configuration
            configuration.GetSection("SemanticAnalyzer").Bind(options);
            
            // Apply custom configuration
            configureSemanticOptions?.Invoke(options);
            
            // Fallback to root level config for backwards compatibility
            options.ApiKey ??= configuration.GetValue<string>("SemanticApiKey");
            options.Endpoint ??= configuration.GetValue<string>("SemanticEndpoint");
        });

        services.AddKeyedScoped<IAnalyzerService, SemanticAnalyzerService>(
            EAnalyzers.AIFoundry,
            (sp, key) =>
            {
                var options = sp.GetRequiredService<IOptions<SemanticAnalyzerOptions>>().Value;
                
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    throw new InvalidOperationException(
                        "SemanticApiKey is required. Configure it via SemanticAnalyzer:ApiKey or SemanticApiKey in configuration.");
                }

                return new SemanticAnalyzerService(options);
            });

        // Register the resolver that picks the right analyzer based on configuration
        services.AddScoped<IAnalyzerServiceResolver, AnalyzerServiceResolver>();

        // Register the orchestrator that coordinates Git and AI services
        services.AddScoped<PullRequestAnalysisOrchestrator>();

        return services;
    }
}
