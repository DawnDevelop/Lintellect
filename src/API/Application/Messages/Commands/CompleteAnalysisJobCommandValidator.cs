using FluentValidation;

namespace devops_pr_analyzer.Application.Messages.Commands;

/// <summary>
/// Validator for CompleteAnalysisJobCommand following CleanArchitecture pattern.
/// </summary>
public sealed class CompleteAnalysisJobCommandValidator : AbstractValidator<CompleteAnalysisJobCommand>
{
  public CompleteAnalysisJobCommandValidator()
  {
    RuleFor(x => x.JobId)
        .NotEmpty()
        .WithMessage("Job ID is required.");

    RuleFor(x => x.Summary)
        .NotNull()
        .NotEmpty()
        .WithMessage("Summary is required.");

    RuleFor(x => x.DetailedAnalysis)
        .NotNull()
        .NotEmpty()
        .WithMessage("Detailed analysis is required.");

    RuleFor(x => x.AnalyzerUsed)
        .NotNull()
        .NotEmpty()
        .MaximumLength(100)
        .WithMessage("Analyzer used must be specified and not exceed 100 characters.");

    RuleFor(x => x.InlineSuggestions)
        .MaximumLength(1000)
        .When(x => !string.IsNullOrEmpty(x.InlineSuggestions))
        .WithMessage("Inline suggestions must not exceed 1000 characters.");
  }
}
