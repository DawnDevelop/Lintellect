using devops_pr_analyzer.Application.Common.Exceptions;
using FluentValidation;
using Mediator;
using ValidationException = devops_pr_analyzer.Application.Common.Exceptions.ValidationException;

namespace devops_pr_analyzer.Application.Common.Behaviors;

/// <summary>
/// Validation behavior for Mediator requests following CleanArchitecture pattern.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators, ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                logger.LogWarning("Validation failed: {@Failures}", failures);
                throw new ValidationException(failures);
            }
        }

        return await next(request, cancellationToken);
    }
}
