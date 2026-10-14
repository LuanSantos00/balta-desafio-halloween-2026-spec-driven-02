using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Api.Data;

public sealed class LinkDbContext(DbContextOptions<LinkDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<Link>();

        link.HasKey(entity => entity.Id);
        link.Property(entity => entity.Code)
            .IsRequired()
            .HasMaxLength(20);
        link.Property(entity => entity.TargetUrl)
            .IsRequired()
            .HasMaxLength(2048);
        link.Property(entity => entity.ManagementKeyHash)
            .IsRequired();
        link.Property(entity => entity.AccessCount)
            .HasDefaultValue(0L);
        link.HasIndex(entity => entity.Code)
            .IsUnique();
    }
}
