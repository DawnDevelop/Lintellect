using FluentValidation;
using Lintellect.Api.Application.Interfaces;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

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
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Git information is required.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.ProjectName)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => x.AnalysisRequest != null && x.AnalysisRequest.GitInfo != null)
            .WithMessage("Project name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.RepositoryName)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => x.AnalysisRequest != null && x.AnalysisRequest.GitInfo != null)
            .WithMessage("Repository name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.PullRequestId)
            .GreaterThan(0)
            .When(x => x.AnalysisRequest != null && x.AnalysisRequest.GitInfo != null)
            .WithMessage("Pull request ID must be a valid positive integer.");

        RuleFor(x => x.AnalysisRequest.GitProvider)
            .IsInEnum()
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Git provider must be a valid provider.");

        RuleFor(x => x.AnalysisRequest.FileExclusions)
            .Must(exclusions => exclusions == null || exclusions.Count <= 50)
            .When(x => x.AnalysisRequest != null)
            .WithMessage("File exclusions must not exceed 50 items.");

        // Validate Git provider credentials with actual connection testing
        RuleFor(x => x.AnalysisRequest)
            .CustomAsync(async (request, context, cancellationToken) =>
            {
                if (request != null)
                {
                    var (IsValid, ErrorMessages) = await ValidateGitCredentialsAsync(request, cancellationToken);
                    if (!IsValid)
                    {
                        ErrorMessages.ForEach(error => context.AddFailure(error));
                    }
                }
            });

        // If Azure DevOps org URL is provided (by request or config), ensure it is a valid URI when present in the request
        When(x => x.AnalysisRequest != null && !string.IsNullOrWhiteSpace(x.AnalysisRequest.AzureDevOpsOrgUrl), () => RuleFor(x => x.AnalysisRequest.AzureDevOpsOrgUrl)
                .Must(BeValidUri)
                .WithMessage("AzureDevOpsOrgUrl must be a valid absolute URI (e.g., https://dev.azure.com/yourorg)."));

        // Validate that at least one AI feature is enabled
        RuleFor(x => x.AnalysisRequest)
            .Must(request => request != null && (request.EnableSummaryComment ||
                           request.EnableDescriptionSummary ||
                           request.EnableInlineSuggestions ||
                           request.EnableAzureDevopsCodeOwners))
            .WithMessage("At least one AI analysis feature must be enabled (EnableAzureDevopsCodeOwners, EnableSummaryComment, EnableDescriptionSummary, or EnableInlineSuggestions).");
    }

    private async Task<(bool IsValid, List<string> ErrorMessages)> ValidateGitCredentialsAsync(Shared.Models.AnalysisRequest request, CancellationToken cancellationToken)
    {
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
            return (false, [$"Credential validation failed: {ex.Message}"]);
        }
    }

    private static bool BeValidUri(string? uriString)
    {
        return !string.IsNullOrWhiteSpace(uriString) && Uri.TryCreate(uriString, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
