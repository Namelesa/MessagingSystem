using MessagingSystem.Services.Messaging.WebApi.Group.Info;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages.Chat;
using MessagingSystem.Services.Messaging.WebApi.Messages.User;

namespace MessagingSystem.Services.Messaging.WebApi;

public static class AddWebApi
{
    public static void AddWebApiLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddAutoMapper(config => config.AddProfile(new GroupMap()));
        services.AddAutoMapper(config => config.AddProfile(new GroupMessagesMapper()));
        services.AddAutoMapper(config => config.AddProfile(new MessageMap()));
        services.AddAutoMapper(config => config.AddProfile(new ChatMap()));
        services.AddAutoMapper(config => config.AddProfile(new UserMap()));
    }
}