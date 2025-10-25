using FluentValidation;
using devops_pr_analyzer.Application.Interfaces;
using devops_pr_analyzer.Infrastructure.Services.Git;
using System.Net.Http;

namespace devops_pr_analyzer.Application.Messages.Commands;

/// <summary>
/// Validator for SubmitAnalysisCommand following CleanArchitecture pattern.
/// </summary>
public sealed class SubmitAnalysisCommandValidator : AbstractValidator<SubmitAnalysisCommand>
{
    private readonly IGitClientFactory _gitClientFactory;

    public SubmitAnalysisCommandValidator(IGitClientFactory gitClientFactory)
    {
        _gitClientFactory = gitClientFactory ?? throw new ArgumentNullException(nameof(gitClientFactory));

        RuleFor(x => x.AnalysisRequest)
            .NotNull()
            .WithMessage("Analysis request is required.");

        RuleFor(x => x.AnalysisRequest.GitInfo)
            .NotNull()
            .WithMessage("Git information is required.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.ProjectName)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Project name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.RepositoryName)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("Repository name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.PullRequestId)
            .GreaterThan(0)
            .WithMessage("Pull request ID must be a valid positive integer.");

        RuleFor(x => x.AnalysisRequest.GitProvider)
            .IsInEnum()
            .WithMessage("Git provider must be a valid provider.");

        RuleFor(x => x.AnalysisRequest.FileExclusions)
            .Must(exclusions => exclusions == null || exclusions.Count <= 50)
            .WithMessage("File exclusions must not exceed 50 items.");

        // Validate Git provider credentials with actual connection testing
        RuleFor(x => x.AnalysisRequest)
            .CustomAsync(async (request, context, cancellationToken) =>
            {
                var (IsValid, ErrorMessages) = await ValidateGitCredentialsAsync(request, cancellationToken);
                if (!IsValid)
                {
                    ErrorMessages.ForEach(error => context.AddFailure(error));
                }
            });

        // Validate Azure DevOps credentials format if provided
        When(x => !string.IsNullOrWhiteSpace(x.AnalysisRequest.DevopsPat) || !string.IsNullOrWhiteSpace(x.AnalysisRequest.AzureDevOpsOrgUrl), () =>
        {
            RuleFor(x => x.AnalysisRequest.DevopsPat)
                .NotEmpty()
                .WithMessage("DevopsPat is required when Azure DevOps credentials are provided.");

            RuleFor(x => x.AnalysisRequest.AzureDevOpsOrgUrl)
                .NotEmpty()
                .WithMessage("AzureDevOpsOrgUrl is required when Azure DevOps credentials are provided.")
                .Must(BeValidUri)
                .WithMessage("AzureDevOpsOrgUrl must be a valid absolute URI (e.g., https://dev.azure.com/yourorg).");
        });

        // Validate GitHub credentials format if provided
        When(x => !string.IsNullOrWhiteSpace(x.AnalysisRequest.GitHubToken), () =>
        {
            RuleFor(x => x.AnalysisRequest.GitHubToken)
                .NotEmpty()
                .WithMessage("GitHubToken cannot be empty if provided.");
        });

        // Validate that at least one AI feature is enabled
        RuleFor(x => x.AnalysisRequest)
            .Must(request => request.EnableSummaryComment ||
                           request.EnableDescriptionSummary ||
                           request.EnableInlineSuggestions ||
                           request.EnableAzureDevopsCodeOwners)
            .WithMessage("At least one AI analysis feature must be enabled (EnableAzureDevopsCodeOwners, EnableSummaryComment, EnableDescriptionSummary, or EnableInlineSuggestions).");
    }

    private async Task<(bool IsValid, List<string> ErrorMessages)> ValidateGitCredentialsAsync(devops_pr_analyzer.shared.Models.AnalysisRequest request, CancellationToken cancellationToken)
    {
        // Check if we have at least one complete set of credentials
        var hasAzureDevOpsCredentials = !string.IsNullOrWhiteSpace(request.DevopsPat) &&
                                   !string.IsNullOrWhiteSpace(request.AzureDevOpsOrgUrl);

        var hasGitHubCredentials = !string.IsNullOrWhiteSpace(request.GitHubToken);

        if (!hasAzureDevOpsCredentials && !hasGitHubCredentials)
        {
            return (false, ["No Git provider credentials provided. Please provide either Azure DevOps PAT and Org URL, or GitHub token."]);
        }

        try
        {
            // Test the actual connection and permissions
            var gitClient = _gitClientFactory.CreateClient(request);
            var permissionResults = await gitClient.HasSufficientPermissionsAsync(request);

            // Check if all required permissions are available
            var failedPermissions = permissionResults.Where(p => !p.HasPermission).ToList();
            if (failedPermissions.Count != 0)
            {
                var errorMessages = failedPermissions.Select(p => p.Reason!).Where(r => !string.IsNullOrEmpty(r)).ToList();
                return (false, errorMessages);
            }

            return (true, []); // Connection and permissions successful
        }
        catch (Exception ex)
        {
            return (false, [$"Permission validation failed: {ex.Message}"]);
        }
    }

    private static bool BeValidUri(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return false;

        return Uri.TryCreate(uriString, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
