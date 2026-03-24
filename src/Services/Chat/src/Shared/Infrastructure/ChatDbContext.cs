using Chat.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chat.Shared.Infrastructure;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedById).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SenderName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();

            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.SessionId, e.CreatedAt })
                  .IsDescending(false, true)
                  .HasDatabaseName("IX_ChatMessages_SessionId_CreatedAt");
        });
    }
}
