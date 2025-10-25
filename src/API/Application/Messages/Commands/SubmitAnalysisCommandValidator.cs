using FluentValidation;

namespace devops_pr_analyzer.Application.Messages.Commands;

/// <summary>
/// Validator for SubmitAnalysisCommand following CleanArchitecture pattern.
/// </summary>
public sealed class SubmitAnalysisCommandValidator : AbstractValidator<SubmitAnalysisCommand>
{
    public SubmitAnalysisCommandValidator()
    {
        // Validate CLI analysis result if provided
        RuleFor(x => x.AnalysisRequest)
            .NotNull()
            .When(x => x.AnalysisRequest != null)
            .WithMessage("CLI analysis result is required when provided.");

        RuleFor(x => x.AnalysisRequest.GitInfo)
            .NotNull()
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Git information is required in the CLI analysis result.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.ProjectName)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Project name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.RepositoryName)
            .NotEmpty()
            .MaximumLength(255)
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Repository name is required and must not exceed 255 characters.");

        RuleFor(x => x.AnalysisRequest.GitInfo!.PullRequestId)
            .GreaterThan(0)
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Pull request ID must be a valid positive integer.");

        RuleFor(x => x.AnalysisRequest.GitProvider)
            .IsInEnum()
            .When(x => x.AnalysisRequest != null)
            .WithMessage("Git provider must be a valid provider.");

        RuleFor(x => x.AnalysisRequest.FileExclusions)
            .Must(exclusions => exclusions == null || exclusions.Count <= 50)
            .WithMessage("File exclusions must not exceed 50 items.");
    }
}
