using devops_pr_analyzer.Application.Messages.Commands;
using devops_pr_analyzer.Domain.Entities;
using devops_pr_analyzer.Domain.Enums;
using devops_pr_analyzer.Infrastructure.Services;
using devops_pr_analyzer.shared.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace devops_pr_analyzer.Infrastructure.Services;

/// <summary>
/// Background service that processes analysis jobs from the queue.
/// </summary>
public sealed class AnalysisBackgroundService(
    AnalysisJobQueue jobQueue,
    IServiceProvider serviceProvider,
    ILogger<AnalysisBackgroundService> logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Analysis background service started");

        await foreach (var job in jobQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing job {JobId}", job.Id);
                // Note: We can't update job status here since we don't have dbContext in scope
                // The job will remain in Pending status, which can be handled by a cleanup process
                logger.LogWarning("Job {JobId} failed but could not update status in database", job.Id);
            }
        }

        logger.LogInformation("Analysis background service stopped");
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        // Get CLI analysis result from JsonDocument
        var cliAnalysisResult = job.AnalysisRequest?.Deserialize<AnalysisRequest>();

        logger.LogInformation("Processing job {JobId} for {Project}/{Repository} PR #{PullRequest}",
            job.Id,
            cliAnalysisResult?.GitInfo?.ProjectName ?? "Unknown",
            cliAnalysisResult?.GitInfo?.RepositoryName ?? "Unknown",
            cliAnalysisResult?.GitInfo?.PullRequestId ?? 0);

        // Create a scoped service provider for this job
        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            // Update job status to Running using MediatR
            await mediator.Send(new UpdateAnalysisJobStatusCommand(
                job.Id,
                AnalysisStatus.Running,
                StartedAt: DateTimeOffset.UtcNow),
                cancellationToken);


            // Perform actual analysis using MediatR command
            var analysisReport = await mediator.Send(new ProcessAnalysisJobCommand(
                job.Id,
                cliAnalysisResult!),
                cancellationToken);

            // Update job with real results using MediatR
            await mediator.Send(new CompleteAnalysisJobCommand(
                job.Id,
                analysisReport.Summary ?? "No summary generated",
                analysisReport.DetailedAnalysis ?? "No detailed analysis generated",
                "Inline suggestions would be posted to PR", // This is handled by the orchestrator
                analysisReport.AnalyzerUsed ?? "Unknown"),
                cancellationToken);

            logger.LogInformation("Successfully processed job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process job {JobId}", job.Id);
            // Update job status to Failed using MediatR
            await mediator.Send(new UpdateAnalysisJobStatusCommand(
                job.Id,
                AnalysisStatus.Failed,
                ErrorMessage: ex.Message),
                cancellationToken);
        }
    }

}