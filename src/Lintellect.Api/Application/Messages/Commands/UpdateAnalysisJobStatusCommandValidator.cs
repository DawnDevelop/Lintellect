using FluentValidation;
using Lintellect.Api.Domain.Enums;

namespace Lintellect.Api.Application.Messages.Commands;

/// <summary>
/// Validator for UpdateAnalysisJobStatusCommand following CleanArchitecture pattern.
/// </summary>
public sealed class UpdateAnalysisJobStatusCommandValidator : AbstractValidator<UpdateAnalysisJobStatusCommand>
{
  public UpdateAnalysisJobStatusCommandValidator()
  {
    RuleFor(x => x.JobId)
        .NotEmpty()
        .WithMessage("Job ID is required.");

    RuleFor(x => x.Status)
        .IsInEnum()
        .WithMessage("Status must be a valid enum value.");

    RuleFor(x => x.ErrorMessage)
        .NotEmpty()
        .When(x => x.Status == AnalysisStatus.Failed)
        .WithMessage("Error message is required when status is Failed.");

    RuleFor(x => x.ErrorMessage)
        .MaximumLength(1000)
        .When(x => !string.IsNullOrEmpty(x.ErrorMessage))
        .WithMessage("Error message must not exceed 1000 characters.");

    RuleFor(x => x.StartedAt)
        .NotNull()
        .When(x => x.Status == AnalysisStatus.Running)
        .WithMessage("Started at time is required when status is Running.");
  }
}
