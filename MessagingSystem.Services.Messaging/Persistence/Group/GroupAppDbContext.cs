using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group;

public class GroupAppDbContext(DbContextOptions<GroupAppDbContext> options) : DbContext(options)
{
    public DbSet<GroupInfo> GroupInfos { get; init; }
    public DbSet<GroupMessage> GroupMessages { get; init; }
    public DbSet<GroupMembers> GroupMembers { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GroupMessage>(builder =>
        {
            builder.Property(u => u.Sender)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.Content)
                .HasMaxLength(3500)
                .IsRequired();
            builder.Property(u => u.SenderHash)
                .HasMaxLength(120)
                .IsRequired();
        });
        
        modelBuilder.Entity<GroupInfo>(builder =>
        {
            builder.Property(u => u.GroupName)
                .HasMaxLength(500)
                .IsRequired();
            builder.Property(u => u.Image)
                .HasMaxLength(250);
            builder.Property(u => u.Description)
                .HasMaxLength(600);
            builder.Property(u => u.Admin)
                .HasMaxLength(80)
                .IsRequired();
            builder.Property(u => u.AdminHash)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.GroupNameHash)
                .HasMaxLength(200)
                .IsRequired();
        });
        
        modelBuilder.Entity<GroupMembers>(builder =>
        {
            builder.Property(u => u.UserNickName)
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(u => u.UserNickNameHash)
                .HasMaxLength(120)
                .IsRequired();
        });
        
        modelBuilder.Entity<GroupMessage>()
            .HasOne(gm => gm.Group)
            .WithMany()
            .HasForeignKey(gm => gm.GroupId);
        
        modelBuilder.Entity<GroupInfo>()
            .HasIndex(u => u.Id)
            .IsUnique();
        modelBuilder.Entity<GroupInfo>()
            .HasIndex(u => u.GroupNameHash)
            .IsUnique();
        modelBuilder.Entity<GroupInfo>()
            .Property(g => g.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        
        modelBuilder.Entity<GroupInfo>()
            .HasMany(g => g.Members)
            .WithOne(m => m.Group)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}