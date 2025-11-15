using FluentValidation;

namespace Lintellect.Api.Application.Messages.Commands.Analysis;

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

        RuleFor(x => x.AnalyzerUsed)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Analyzer used must be specified and not exceed 100 characters.");
    }
}
