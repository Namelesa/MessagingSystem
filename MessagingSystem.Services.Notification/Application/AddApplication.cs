using FluentValidation;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application;

public static class AddApplication
{
    public static void AddApplicationLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IValidator<UserDto>, UserValidator>();
        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
    }
}