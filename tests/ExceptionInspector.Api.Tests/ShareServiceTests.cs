using ExceptionInspector.Api.Data;
using ExceptionInspector.Api.Models;
using ExceptionInspector.Api.Services;
using ExceptionInspector.Core.Analysis;
using Microsoft.EntityFrameworkCore;

namespace ExceptionInspector.Api.Tests;

public sealed class ShareServiceTests
{
  [Fact]
  public async Task CreateAndGetReturnsRedactedReport()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(dbContext);

    var created = await service.CreateAsync(
      new CreateShareRequest
      {
        Text =
          "System.Exception: Contact alice@example.com\n" +
          "   at Orders.Load() in /Users/dev/src/Orders.cs:line 42",
      },
      CancellationToken.None);

    var shared = await service.GetAsync(
      created.Slug,
      CancellationToken.None);

    Assert.NotNull(shared);
    Assert.False(string.IsNullOrWhiteSpace(shared.Report.AnalysisId));
    Assert.Equal(
      "System.Exception",
      shared.Report.Verdict.ExceptionType);
    Assert.Contains("<email>", shared.Report.Verdict.ExceptionMessage);

    var entity = await dbContext.Shares.SingleAsync(
      TestContext.Current.CancellationToken);
    Assert.DoesNotContain("alice@example.com", entity.ReportJson);
    Assert.NotEqual(created.DeleteToken, entity.DeleteTokenHash);
    Assert.InRange(
      (created.ExpiresAt - DateTimeOffset.UtcNow).TotalDays,
      6.99,
      7);
  }

  [Fact]
  public async Task DeleteWithWrongTokenKeepsShare()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(dbContext);
    var created = await CreateShareAsync(service);

    var deleted = await service.DeleteAsync(
      created.Slug,
      "wrong-token",
      CancellationToken.None);

    Assert.False(deleted);
    Assert.NotNull(await service.GetAsync(created.Slug, CancellationToken.None));
  }

  [Fact]
  public async Task DeleteWithCorrectTokenRemovesShare()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(dbContext);
    var created = await CreateShareAsync(service);

    var deleted = await service.DeleteAsync(
      created.Slug,
      created.DeleteToken,
      CancellationToken.None);

    Assert.True(deleted);
    Assert.Null(await service.GetAsync(created.Slug, CancellationToken.None));
  }

  [Fact]
  public async Task GetExpiredShareRemovesItAndReturnsNotFound()
  {
    await using var dbContext = CreateDbContext();
    var service = CreateService(dbContext);
    var created = await CreateShareAsync(service);
    var entity = await dbContext.Shares.SingleAsync(
      TestContext.Current.CancellationToken);
    entity.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
    await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

    var shared = await service.GetAsync(
      created.Slug,
      CancellationToken.None);

    Assert.Null(shared);
    Assert.Empty(dbContext.Shares);
  }

  private static async Task<CreateShareResult> CreateShareAsync(
    ShareService service)
  {
    return await service.CreateAsync(
      new CreateShareRequest
      {
        Text = "System.Exception: Test failure\n   at Orders.Load()",
      },
      CancellationToken.None);
  }

  private static ShareService CreateService(
    ExceptionInspectorDbContext dbContext)
  {
    return new ShareService(
      dbContext,
      new ParserAnalysisService(new ExceptionAnalyzer()));
  }

  private static ExceptionInspectorDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<ExceptionInspectorDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new ExceptionInspectorDbContext(options);
  }
}
