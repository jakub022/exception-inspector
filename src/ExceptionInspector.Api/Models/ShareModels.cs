using System.ComponentModel.DataAnnotations;

namespace ExceptionInspector.Api.Models;

/// <summary>Payload for creating a seven-day share link.</summary>
public sealed record CreateShareRequest
{
  /// <summary>The exception text to analyze and store in redacted form.</summary>
  [Required, MinLength(1), MaxLength(200_000)]
  public required string Text { get; init; }
}

/// <summary>Share-link details returned once after creation.</summary>
public sealed record CreateShareResponse(
  string Slug,
  string Url,
  DateTimeOffset ExpiresAt,
  string DeleteToken);

/// <summary>A previously created, non-expired shared report.</summary>
public sealed record SharedReportResponse(
  AnalysisReport Report,
  DateTimeOffset ExpiresAt);
