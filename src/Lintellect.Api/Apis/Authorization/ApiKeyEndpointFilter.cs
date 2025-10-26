using Lintellect.Api.Apis.Options;
using Microsoft.Extensions.Options;

namespace Lintellect.Api.Apis.Authorization;

public class ApiKeyEndpointFilter(
    IOptions<AuthorizationOptions> options) : IEndpointFilter
{
    public const string ApiKeyHeaderName = "Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return TypedResults.Unauthorized();
        }

        var configuredApiKey = options.Value.ApiKey;

        return string.IsNullOrEmpty(configuredApiKey) || configuredApiKey != extractedApiKey
            ? TypedResults.Unauthorized()
            : await next(context);
    }
}
