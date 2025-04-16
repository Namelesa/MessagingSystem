using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Persistence;

public static class AddPersistence
{
    public static void AddPersistenceLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<INotification, Notification>();
    }
}