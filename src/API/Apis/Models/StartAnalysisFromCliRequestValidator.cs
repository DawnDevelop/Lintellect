using FluentValidation;
using devops_pr_analyzer.shared.Models;

namespace devops_pr_analyzer.Apis.Models;

/// <summary>
/// Validator for StartAnalysisFromCliRequest following CleanArchitecture pattern.
/// </summary>
public sealed class StartAnalysisFromCliRequestValidator : AbstractValidator<StartAnalysisFromCliRequest>
{
    public StartAnalysisFromCliRequestValidator()
    {
        RuleFor(x => x.AnalysisResult)
            .NotNull()
            .WithMessage("Analysis result is required.");

        RuleFor(x => x.AnalysisResult.GitInfo)
            .NotNull()
            .WithMessage("Git information is required in the analysis result.");

        RuleFor(x => x.AnalysisResult.GitInfo!.ProjectName)
            .NotEmpty()
            .WithMessage("Project name is required and cannot be empty.");

        RuleFor(x => x.AnalysisResult.GitInfo!.RepositoryName)
            .NotEmpty()
            .WithMessage("Repository name is required and cannot be empty.");

        RuleFor(x => x.AnalysisResult.GitInfo!.PullRequestId)
            .GreaterThan(0)
            .WithMessage("Pull request ID must be a valid positive integer.");

        RuleFor(x => x.AnalysisResult.GitProvider)
            .IsInEnum()
            .WithMessage("Git provider must be a valid provider.");

        RuleFor(x => x.AnalysisResult.FileExclusions)
            .Must(exclusions => exclusions == null || exclusions.Count <= 50)
            .WithMessage("File exclusions must not exceed 50 items.");
    }

}
