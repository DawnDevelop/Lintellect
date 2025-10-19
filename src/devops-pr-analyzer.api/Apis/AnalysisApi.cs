using devops_pr_analyzer.Apis.Authorization;
using devops_pr_analyzer.shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace devops_pr_analyzer.Apis;

public static class AnalysisApi
{
    public static IEndpointRouteBuilder MapAnalysisApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/analysis")
            .WithTags("Analysis")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        api.MapPost("/result", PostAnalysisResult)
            .WithName("PostAnalysisResult")
            .WithSummary("Submit analysis result")
            .WithDescription("Submit the result of a static analysis run.");

        return app;
    }

    private static async Task<Ok> PostAnalysisResult(
        AnalysisResult analysisResult)
    {
        // TODO: Implement analysis result processing logic
        await Task.CompletedTask;
        return TypedResults.Ok();
    }
}
