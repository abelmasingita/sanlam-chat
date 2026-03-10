using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.MessageId);
                entity.Property(m => m.Username).IsRequired().HasMaxLength(100);
                entity.Property(m => m.Content).IsRequired().HasMaxLength(2000);
                entity.Property(m => m.SessionId).IsRequired();
                entity.Property(m => m.SentAt).IsRequired();
                // Index to avoid full table scan on the ORDER BY SentAt DESC LIMIT 50 query.
                entity.HasIndex(m => m.SentAt);
            });
        }
    }
}
