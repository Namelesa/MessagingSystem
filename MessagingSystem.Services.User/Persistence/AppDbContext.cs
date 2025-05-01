using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.User.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Core.User.User> Users { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Core.User.User>(builder =>
        {
            builder.Property(u => u.Login)
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(u => u.NickName)
                .HasMaxLength(80)
                .IsRequired();
            
            builder.Property(u => u.HashLogin)
                .HasMaxLength(120)
                .IsRequired();
            
            builder.Property(u => u.HashEmail)
                .HasMaxLength(120)
                .IsRequired();
            
            builder.Property(u => u.HashNickName)
                .HasMaxLength(120)
                .IsRequired();
        });
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(u => u.HashLogin)
            .IsUnique();
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(r => r.HashEmail)
            .IsUnique();
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(r => r.HashNickName)
            .IsUnique();
    }
}