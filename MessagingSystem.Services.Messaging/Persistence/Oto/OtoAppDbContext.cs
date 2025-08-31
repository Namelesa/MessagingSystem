using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto;

public class OtoAppDbContext(DbContextOptions<OtoAppDbContext> options) : DbContext(options)
{
    public DbSet<Message> UsersMessages { get; init; }
    public DbSet<UserImage> Images { get; init; }

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
                .HasMaxLength(20000)
                .IsRequired();
            builder.Property(u => u.RecipientHash)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.SenderHash)
                .HasMaxLength(120)
                .IsRequired();
        });

        modelBuilder.Entity<Message>()
            .HasIndex(u => u.Id);
        modelBuilder.Entity<Message>()
            .HasIndex(u => u.RecipientHash);
        modelBuilder.Entity<Message>()
            .HasIndex(u => u.SenderHash);
        
        modelBuilder.Entity<UserImage>(builder =>
        {
            builder.Property(u => u.NickNameHash)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.Image)
                .HasMaxLength(500)
                .IsRequired();
        });
        
        modelBuilder.Entity<UserImage>()
            .HasIndex(u => u.Id);
        modelBuilder.Entity<UserImage>()
            .HasIndex(u => u.NickNameHash)
            .IsUnique();
    }
}