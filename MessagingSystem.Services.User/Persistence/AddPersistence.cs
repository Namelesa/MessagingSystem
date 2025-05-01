using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Persistence.DbInitializer;
using MessagingSystem.Services.User.Persistence.User;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.User.Persistence;

public static class AddPersistence
{
    public static void AddPersistenceLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDbInitializer, DbInitializer.DbInitializer>();
        services.AddLogging();
    }
}