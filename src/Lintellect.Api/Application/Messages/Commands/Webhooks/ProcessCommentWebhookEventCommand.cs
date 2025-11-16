using System.Text.Json;
using Lintellect.Api.Application.Interfaces;
using Lintellect.Api.Application.Models;
using Lintellect.Api.Application.Models.Git;
using Lintellect.Api.Application.Models.Webhooks;
using Lintellect.Api.Domain.Entities;
using Lintellect.Api.Infrastructure.Services.Git;
using Lintellect.Shared.Models;
using Mediator;

namespace Lintellect.Api.Application.Messages.Commands.Webhooks;

/// <summary>
/// Command to process a comment webhook event from a Git provider.
/// </summary>
public sealed record ProcessCommentWebhookEventCommand(WebhookEvent WebhookEvent) : IRequest;

/// <summary>
/// Processes webhook comment events and answers questions from comments.
/// </summary>
public sealed class ProcessWebhookEventCommandHandler(
    ILogger<ProcessWebhookEventCommandHandler> logger,
    PullRequestService pullRequestService,
    IAnalyzerServiceResolver analyzerResolver
    ) : IRequestHandler<ProcessCommentWebhookEventCommand>
{

    public async ValueTask<Unit> Handle(ProcessCommentWebhookEventCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.WebhookEvent);

        logger.LogInformation(
            "Processing comment webhook event {WebhookId} from {Provider}",
            request.WebhookEvent.Id,
            request.WebhookEvent.Provider);

        try
        {
            switch (request.WebhookEvent.Provider)
            {
                case EGitProvider.AzureDevops:
                    await HandleAzureDevOpsCommentAsync(request.WebhookEvent, cancellationToken);
                    break;
                case EGitProvider.GitHub:
                    await HandleGitHubCommentAsync(request.WebhookEvent, cancellationToken);
                    break;
                default:
                    logger.LogWarning("Unsupported Git provider: {Provider}", request.WebhookEvent.Provider);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing comment webhook event {WebhookId}", request.WebhookEvent.Id);
            throw;
        }

        return Unit.Value;
    }

    private async Task HandleAzureDevOpsCommentAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var prCommentEvent = webhookEvent.EventPayload.Deserialize<PullRequestCommentEvent>();

        if (prCommentEvent == null)
        {
            logger.LogWarning("Failed to deserialize Azure DevOps comment event for webhook {WebhookId}", webhookEvent.Id);
            return;
        }

        // Extract comment content from the event
        var resource = prCommentEvent.Resource;
        var commentContent = resource.Comment.Content;

        // Check if this is a question (mentions bot or is a question)
        if (!IsQuestion(commentContent))
        {
            logger.LogDebug("Comment does not appear to be a question, skipping");
            return;
        }

        // Create AnalysisRequest from webhook data
        var analysisRequest = CreateAnalysisRequestFromAzureDevOpsEvent(resource, webhookEvent.Provider);

        // Extract question (remove mention if present)
        var question = ExtractQuestion(commentContent);

        var threadContext = await pullRequestService.GetPullRequestThreadAsync(analysisRequest, int.Parse(prCommentEvent.Resource.PullRequest.Links.Threads.Href.Split('/').Last()));
        var customInstructions = await pullRequestService.GetCustomInstructionsAsync(analysisRequest);

        var context = BuildQuestionContext(question, [.. threadContext.Comments], customInstructions);

        // Answer the question
        await AnswerQuestionAsync(analysisRequest, question, context, cancellationToken);
    }

    private Task HandleGitHubCommentAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {

        logger.LogInformation("GitHub comment handling not yet implemented for webhook {WebhookId}", webhookEvent.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates an AnalysisRequest from Azure DevOps webhook event data.
    /// </summary>
    private static AnalysisRequest CreateAnalysisRequestFromAzureDevOpsEvent(
        PullRequestCommentResource resource,
        EGitProvider provider)
    {
        var repository = resource.PullRequest.Repository;
        var project = repository.Project;


        var gitInfo = new GitInfo(
            PullRequestId: resource.PullRequest.PullRequestId,
            CommitId: resource.PullRequest.LastMergeSourceCommit.CommitId,
            RepositoryName: repository.Name,
            Type: EGitInfoType.PullRequest,
            ProjectName: project.Name);

        return new AnalysisRequest
        {
            GitInfo = gitInfo,
            GitProvider = provider,
        };
    }

    /// <summary>
    /// Determines if a comment is a question that should be answered.
    /// </summary>
    private static bool IsQuestion(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return false;

        var trimmed = comment.Trim();

        // Check if bot is mentioned
        if (trimmed.Contains("Lintellect", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("lintellect", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if it starts with question words
        var questionWords = new[] { "explain", "what", "how", "why", "can you", "help", "tell me", "?" };
        return questionWords.Any(word => trimmed.StartsWith(word, StringComparison.OrdinalIgnoreCase)) ||
               trimmed.EndsWith("?", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the question text, removing mentions if present.
    /// </summary>
    private static string ExtractQuestion(string comment)
    {
        var trimmed = comment.Trim();

        // Remove @Lintellect mention if present
        var mentionIndex = trimmed.IndexOf("@lintellect", StringComparison.OrdinalIgnoreCase);
        if (mentionIndex >= 0)
        {
            trimmed = trimmed[(mentionIndex + "@lintellect".Length)..].Trim();
        }

        return trimmed;
    }


    /// <summary>
    /// Answers a question from a comment, optionally with thread context.
    /// </summary>
    public async Task AnswerQuestionAsync(
      AnalysisRequest request,
      string threadContext,
      string question,
      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        logger.LogInformation("Answering question for PR {PullRequestId}", request.GitInfo?.PullRequestId);

        try
        {
            var analyzer = analyzerResolver.GetAnalyzerService(request.AIAnalyzer);
            var instructions = await pullRequestService.GetCustomInstructionsAsync(request);
            var model = new AnalyzerServiceModel(request, instructions ?? string.Empty);

            var answer = await analyzer.AnswerQuestionAsync(model, threadContext, question, cancellationToken);

            // Post answer back to PR
            await pullRequestService.AddCommentAsync(request, answer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error answering question for PR {PullRequestId}", request.GitInfo?.PullRequestId);
            await pullRequestService.AddCommentAsync(request,
                $"❌ Sorry, I encountered an error while answering your question: {ex.Message}");
        }
    }

    private static string BuildQuestionContext(
        string question,
        List<PullRequestComment> threadContext,
        string? customInstructions)
    {
        var contextBuilder = new System.Text.StringBuilder();

        contextBuilder.AppendLine("## Question");
        contextBuilder.AppendLine(question);
        contextBuilder.AppendLine();

        if (threadContext.Count > 0)
        {
            contextBuilder.AppendLine("## Thread Context");

            foreach (var item in threadContext)
            {
                contextBuilder.AppendLine($"### Comment number {item.Id} by {item.Author?.DisplayName ?? "Unknown"}");
                contextBuilder.AppendLine(item.Content);
                contextBuilder.AppendLine();
            }

        }

        if (customInstructions != null && !string.IsNullOrWhiteSpace(customInstructions))
        {
            contextBuilder.AppendLine("## Project Guidelines");
            contextBuilder.AppendLine(customInstructions);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }
}

