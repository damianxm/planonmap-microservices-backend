using MapItems.Shared.Domain.Common;
using MapItems.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapItems.Shared.Infrastructure;

public class MapItemsDbContext(DbContextOptions<MapItemsDbContext> options) : DbContext(options)
{
    public DbSet<MapMarkerItems> MapMarkerItems => Set<MapMarkerItems>();
    public DbSet<MapSession> MapSessions => Set<MapSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MapSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedById).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<MapMarkerItems>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(MarkerContentRules.MaxNameLength).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(MarkerContentRules.MaxDescriptionLength);

            entity.HasOne(e => e.Session)
                  .WithMany()
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SessionId)
                  .HasDatabaseName("IX_MapMarkerItems_SessionId");
        });
    }
}
