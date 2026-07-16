using Lintellect.Api.Application.Messages.Commands.Analysis;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Domain.Enums;
using Lintellect.Api.Infrastructure.Telemetry;
using Mediator;

namespace Lintellect.Api.Infrastructure.Services.Analysis;

/// <summary>
/// Background service that processes analysis jobs from the queue.
/// </summary>
public sealed class AnalysisBackgroundService(
    AnalysisJobQueue jobQueue,
    IServiceProvider serviceProvider,
    ILogger<AnalysisBackgroundService> logger,
    AnalysisMetrics metrics) : BackgroundService
{
    internal static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Analysis background service started");

        try
        {
            await foreach (var job in jobQueue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Background service is stopping, cancelling job processing");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing job {JobId}", job.Id);
                    // Try to update job status to failed
                    await TryUpdateJobStatusAsync(job.Id, AnalysisStatus.Failed, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Background service stopped due to cancellation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in background service");
        }
        finally
        {
            logger.LogInformation("Analysis background service stopped");
        }
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var analyzerType = job.AnalyzerUsed ?? "Unknown";

        // Get CLI analysis result from JsonDocument
        var cliAnalysisResult = job.AnalysisRequest;

        logger.LogInformation("Processing job {JobId} for {Project}/{Repository} PR #{PullRequest}",
            job.Id,
            cliAnalysisResult?.GitInfo?.ProjectName ?? "Unknown",
            cliAnalysisResult?.GitInfo?.RepositoryName ?? "Unknown",
            cliAnalysisResult?.GitInfo?.PullRequestId ?? 0);

        // Record job submission
        metrics.RecordJobSubmitted(analyzerType);

        // Create a scoped service provider for this job
        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Job timeout; kept above ClaudeAnalyzerService.BatchPollTimeout so a slow Anthropic batch
        // gives up gracefully inside the analyzer instead of being cancelled mid-flight here.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(JobTimeout);

        try
        {
            // Update job status to Running using Mediator
            await mediator.Send(new UpdateAnalysisJobStatusCommand(
                job.Id,
                AnalysisStatus.Running,
                StartedAt: DateTimeOffset.UtcNow),
                timeoutCts.Token);

            var analysisRequest = job.CreateAnalysisRequestSnapshot();

            // Perform actual analysis using Mediator command
            var analysisReport = await mediator.Send(new ProcessAnalysisJobCommand(
                job.Id,
                analysisRequest,
                job.SourceCommitId,
                job.ReanalysisBaseCommitId),
                timeoutCts.Token);

            // Update job with real results using Mediator
            await mediator.Send(new CompleteAnalysisJobCommand(
                job.Id,
                analysisReport.Summary ?? "No summary generated",
                analysisReport.DetailedAnalysis ?? "No detailed analysis generated",
                "Inline suggestions would be posted to PR", // This is handled by the orchestrator
               analyzerType),
                timeoutCts.Token);

            // Record successful completion
            var duration = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
            metrics.RecordJobCompleted(analyzerType, duration);

            logger.LogInformation("Successfully processed job {JobId}", job.Id);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Job {JobId} timed out after {Timeout} minutes", job.Id, JobTimeout.TotalMinutes);

            // Record failed job
            var duration = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
            metrics.RecordJobFailed(analyzerType, "timeout", duration);

            await mediator.Send(new UpdateAnalysisJobStatusCommand(
                job.Id,
                AnalysisStatus.Failed,
                ErrorMessage: $"Job timed out after {JobTimeout.TotalMinutes} minutes"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process job {JobId}", job.Id);

            // Record failed job
            var duration = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
            metrics.RecordJobFailed(analyzerType, "exception", duration);

            await TryUpdateJobStatusAsync(job.Id, AnalysisStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Attempts to update job status with error handling.
    /// </summary>
    private async Task TryUpdateJobStatusAsync(Guid jobId, AnalysisStatus status, string? errorMessage = null)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new UpdateAnalysisJobStatusCommand(
                jobId,
                status,
                ErrorMessage: errorMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job {JobId} status to {Status}", jobId, status);
        }
    }

    /// <summary>
    /// Graceful shutdown override.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping background service, waiting for current jobs...");
        await base.StopAsync(cancellationToken);
    }
}
