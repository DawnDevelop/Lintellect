using FluentValidation;

namespace devops_pr_analyzer.Application.Messages.Commands;

/// <summary>
/// Validator for ProcessAnalysisJobCommand following CleanArchitecture pattern.
/// </summary>
public sealed class ProcessAnalysisJobCommandValidator : AbstractValidator<ProcessAnalysisJobCommand>
{
  public ProcessAnalysisJobCommandValidator()
  {
    RuleFor(x => x.JobId)
        .NotEmpty()
        .WithMessage("Job ID is required.");

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

    // Validate that at least one AI feature is enabled
    RuleFor(x => x.AnalysisRequest)
        .Must(request => request.EnableSummaryComment ||
                       request.EnableDescriptionSummary ||
                       request.EnableInlineSuggestions || 
                       request.EnableCodeOwners)
        .WithMessage("At least one AI analysis feature must be enabled (EnableCodeOwners, EnableSummaryComment, EnableDescriptionSummary, or EnableInlineSuggestions).");
  }
}
