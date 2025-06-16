using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MessagingSystem.Services.Messaging.Persistence;

public static class AddPersistence
{
    public static void AddPersistenceLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OtoAppDbContext>(options => 
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContext<GroupAppDbContext>(options => 
            options.UseNpgsql(configuration.GetConnectionString("GroupDefaultConnection")));

        services.AddSingleton<IDbContextFactory<GroupAppDbContext>>(_ =>
        {
            var connectionString = configuration.GetConnectionString("GroupDefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<GroupAppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new PooledDbContextFactory<GroupAppDbContext>(optionsBuilder.Options);
        });

        services.AddSingleton<IDbContextFactory<OtoAppDbContext>>(_ =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<OtoAppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new PooledDbContextFactory<OtoAppDbContext>(optionsBuilder.Options);
        });
        
        services.AddScoped<IOtoMessageRepository, OtoMessageRepository>();
        services.AddScoped<IGroupInfoRepository, GroupInfoRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IGroupMembersRepository, GroupMemberRepository>();
        services.AddScoped<IGroupMessagesRepository, GroupMessagesRepository>();
        services.AddScoped<IUserImageRepository, UserImageRepository>();
        services.AddScoped<IOtoDbInitializer, OtoDbInitializer>();
        services.AddScoped<IGroupDbInitializer, GroupDbInitializer>();
    }
}