using System.ComponentModel.DataAnnotations;
using ExceptionInspector.Api.Models;
using ExceptionInspector.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ExceptionInspector.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/api/analyze",
                async Task<Results<Ok<AnalysisReport>, BadRequest<ProblemDetails>>> (
                    AnalyzeRequest request,
                    IAnalysisService service,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Text))
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "The exception text is required.",
                            Detail = "Provide an exception or stack trace to analyze."
                        });
                    }

                    if (request.Text.Length > 200_000)
                    {
                        return TypedResults.BadRequest(new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "The exception text is too long.",
                            Detail = "Exception text must be 200,000 characters or fewer."
                        });
                    }

                    var report = await service.AnalyzeAsync(request, cancellationToken);
                    return TypedResults.Ok(report);
                })
            .WithName("AnalyzeException")
            .WithSummary("Analyze a .NET exception")
            .WithDescription("Returns a structured analysis of a pasted .NET exception or stack trace.")
            .Produces<AnalysisReport>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }
}
