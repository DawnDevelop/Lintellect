using Lintellect.Api.Application.Common.Exceptions;
using Mediator;

namespace Lintellect.Api.Application.Common.Behaviors;

public sealed class LoggingBehaviour<TMessage, TResponse>(ILogger<TMessage> logger) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Request: {@Request}", message);

            var response = await next(message, cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "An Error Occured");
            throw;
        }

    }
}
