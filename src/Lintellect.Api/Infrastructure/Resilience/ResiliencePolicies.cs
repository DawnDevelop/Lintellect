using Polly;
using Polly.Extensions.Http;

namespace Lintellect.Api.Infrastructure.Resilience;

/// <summary>
/// Resilience policies for external API calls following CleanArchitecture pattern.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy with exponential backoff for transient HTTP errors.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} in {timespan} seconds due to: {outcome.Exception?.Message}");
                });
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration} due to: {exception.Exception.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }

    /// <summary>
    /// Timeout policy for external API calls.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Combined policy with retry, circuit breaker, and timeout.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(
            GetRetryPolicy(),
            GetCircuitBreakerPolicy(),
            GetTimeoutPolicy());
    }

    /// <summary>
    /// Policy specifically for AI API calls with longer timeout.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetAiApiPolicy()
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMinutes(5));

        return Policy.WrapAsync(retryPolicy, timeoutPolicy);
    }
}
