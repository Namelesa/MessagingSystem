using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto;

public class OtoAppDbContext(DbContextOptions<OtoAppDbContext> options) : DbContext(options)
{
    public DbSet<Message> UsersMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Message>(builder =>
        {
            builder.Property(u => u.Sender)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.Recipient)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.Content)
                .HasMaxLength(3500)
                .IsRequired();
            builder.Property(u => u.RecipientHash)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.SenderHash)
                .HasMaxLength(120)
                .IsRequired();
        });
    }
}