using MessagingSystem.Services.Messaging.Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Message> UsersMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>(builder =>
        {
            builder.Property(u => u.Sender)
                .HasMaxLength(80)
                .IsRequired();
            builder.Property(u => u.Recipient)
                .HasMaxLength(80)
                .IsRequired();
            builder.Property(u => u.Content)
                .HasMaxLength(2048)
                .IsRequired();
        });
    }
}