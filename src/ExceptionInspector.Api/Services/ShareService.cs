using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ExceptionInspector.Api.Data;
using ExceptionInspector.Api.Models;

namespace ExceptionInspector.Api.Services;

public sealed class ShareService : IShareService
{
  private static readonly JsonSerializerOptions JsonOptions = new(
    JsonSerializerDefaults.Web);
  private readonly ExceptionInspectorDbContext dbContext;
  private readonly IAnalysisService analysisService;

  public ShareService(
    ExceptionInspectorDbContext dbContext,
    IAnalysisService analysisService)
  {
    this.dbContext = dbContext;
    this.analysisService = analysisService;
  }

  public async Task<CreateShareResult> CreateAsync(
    CreateShareRequest request,
    CancellationToken cancellationToken)
  {
    var report = await analysisService.AnalyzeAsync(
      new AnalyzeRequest { Text = request.Text },
      cancellationToken);
    var now = DateTimeOffset.UtcNow;
    var deleteToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var entity = new ShareEntity
    {
      Slug = CreateSlug(),
      ReportJson = JsonSerializer.Serialize(report, JsonOptions),
      DeleteTokenHash = HashToken(deleteToken),
      CreatedAt = now,
      ExpiresAt = now.AddDays(7),
    };

    dbContext.Shares.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new CreateShareResult(entity.Slug, entity.ExpiresAt, deleteToken);
  }

  public async Task<SharedReportResponse?> GetAsync(
    string slug,
    CancellationToken cancellationToken)
  {
    var entity = await dbContext.Shares.FindAsync(
      [slug],
      cancellationToken);

    if (entity is null)
    {
      return null;
    }

    if (entity.ExpiresAt <= DateTimeOffset.UtcNow)
    {
      dbContext.Shares.Remove(entity);
      await dbContext.SaveChangesAsync(cancellationToken);
      return null;
    }

    var report = JsonSerializer.Deserialize<AnalysisReport>(
      entity.ReportJson,
      JsonOptions);

    return report is null
      ? null
      : new SharedReportResponse(report, entity.ExpiresAt);
  }

  public async Task<bool> DeleteAsync(
    string slug,
    string deleteToken,
    CancellationToken cancellationToken)
  {
    var entity = await dbContext.Shares.FindAsync(
      [slug],
      cancellationToken);

    if (entity is null)
    {
      return false;
    }

    if (entity.ExpiresAt <= DateTimeOffset.UtcNow ||
        !CryptographicOperations.FixedTimeEquals(
          Convert.FromBase64String(entity.DeleteTokenHash),
          HashTokenBytes(deleteToken)))
    {
      if (entity.ExpiresAt <= DateTimeOffset.UtcNow)
      {
        dbContext.Shares.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
      }

      return false;
    }

    dbContext.Shares.Remove(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }

  private static string CreateSlug()
  {
    return Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(12))
      .Replace('+', '-')
      .Replace('/', '_')
      .TrimEnd('=');
  }

  private static string HashToken(string token)
  {
    return Convert.ToBase64String(HashTokenBytes(token));
  }

  private static byte[] HashTokenBytes(string token)
  {
    return SHA256.HashData(Encoding.UTF8.GetBytes(token));
  }
}

public sealed record CreateShareResult(
  string Slug,
  DateTimeOffset ExpiresAt,
  string DeleteToken);
