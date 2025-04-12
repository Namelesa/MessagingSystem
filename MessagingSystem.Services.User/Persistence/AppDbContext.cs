using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.User.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Core.User.User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Core.User.User>(builder =>
        {
            builder.Property(u => u.Login)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.NickName)
                .HasMaxLength(15)
                .IsRequired();
        });
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(u => u.Login)
            .IsUnique();
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(r => r.NickName)
            .IsUnique();
        modelBuilder.Entity<Core.User.User>()
            .HasIndex(r => r.Email)
            .IsUnique();
    }
}