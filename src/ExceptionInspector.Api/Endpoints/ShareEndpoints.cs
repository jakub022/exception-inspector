using ExceptionInspector.Api.Models;
using ExceptionInspector.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ExceptionInspector.Api.Endpoints;

public static class ShareEndpoints
{
  public static void MapShareEndpoints(
    this WebApplication app,
    IConfiguration configuration)
  {
    app.MapPost(
        "/api/shares",
        async Task<Results<Created<CreateShareResponse>, BadRequest<ProblemDetails>>> (
          CreateShareRequest request,
          IShareService service,
          HttpContext httpContext,
          CancellationToken cancellationToken) =>
        {
          if (string.IsNullOrWhiteSpace(request.Text))
          {
            return TypedResults.BadRequest(new ProblemDetails
            {
              Status = StatusCodes.Status400BadRequest,
              Title = "The exception text is required.",
              Detail = "Provide an exception or stack trace to share.",
            });
          }

          var result = await service.CreateAsync(request, cancellationToken);
          var clientBaseUrl = configuration["ClientBaseUrl"]
            ?? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
          var response = new CreateShareResponse(
            result.Slug,
            $"{clientBaseUrl.TrimEnd('/')}/s/{result.Slug}",
            result.ExpiresAt,
            result.DeleteToken);

          return TypedResults.Created($"/api/shares/{result.Slug}", response);
        })
      .WithName("CreateShare")
      .WithSummary("Create a seven-day share link")
      .WithDescription("Stores only the analyzer's redacted report.")
      .Produces<CreateShareResponse>(StatusCodes.Status201Created)
      .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

    app.MapGet(
        "/api/shares/{slug}",
        async Task<Results<Ok<SharedReportResponse>, NotFound>> (
          string slug,
          IShareService service,
          CancellationToken cancellationToken) =>
        {
          var response = await service.GetAsync(slug, cancellationToken);
          return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
        })
      .WithName("GetShare")
      .WithSummary("Get a non-expired shared report")
      .Produces<SharedReportResponse>(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status404NotFound);

    app.MapDelete(
        "/api/shares/{slug}",
        async Task<Results<NoContent, NotFound>> (
          string slug,
          IShareService service,
          [FromHeader(Name = "X-Delete-Token")] string? deleteToken,
          CancellationToken cancellationToken) =>
        {
          if (string.IsNullOrWhiteSpace(deleteToken) ||
              !await service.DeleteAsync(
                slug,
                deleteToken,
                cancellationToken))
          {
            return TypedResults.NotFound();
          }

          return TypedResults.NoContent();
        })
      .WithName("DeleteShare")
      .WithSummary("Delete a shared report")
      .Produces(StatusCodes.Status204NoContent)
      .Produces(StatusCodes.Status404NotFound);
  }
}
