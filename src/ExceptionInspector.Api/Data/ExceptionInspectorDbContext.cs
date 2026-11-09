using Microsoft.EntityFrameworkCore;

namespace ExceptionInspector.Api.Data;

public sealed class ExceptionInspectorDbContext : DbContext
{
  public ExceptionInspectorDbContext(
    DbContextOptions<ExceptionInspectorDbContext> options)
    : base(options)
  {
  }

  public DbSet<ShareEntity> Shares => Set<ShareEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<ShareEntity>(entity =>
    {
      entity.HasKey(share => share.Slug);
      entity.Property(share => share.Slug).HasMaxLength(32);
      entity.Property(share => share.ReportJson).IsRequired();
      entity.Property(share => share.DeleteTokenHash)
        .HasMaxLength(64)
        .IsRequired();
      entity.Property(share => share.CreatedAt).IsRequired();
      entity.Property(share => share.ExpiresAt).IsRequired();
      entity.HasIndex(share => share.ExpiresAt);
    });
  }
}

public sealed class ShareEntity
{
  public string Slug { get; set; } = string.Empty;
  public string ReportJson { get; set; } = string.Empty;
  public string DeleteTokenHash { get; set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; set; }
  public DateTimeOffset ExpiresAt { get; set; }
}
