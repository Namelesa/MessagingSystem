using MessagingSystem.Services.User.WebApi.Login;
using MessagingSystem.Services.User.WebApi.Register;
using MessagingSystem.Services.User.WebApi.User;

namespace MessagingSystem.Services.User.WebApi;

public static class AddWebApi
{
    public static void AddWebApiLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(config => config.AddProfile(new RegisterMap()));
        services.AddAutoMapper(config => config.AddProfile(new LoginMap()));
        services.AddAutoMapper(config => config.AddProfile(new UserMap()));
    }
}