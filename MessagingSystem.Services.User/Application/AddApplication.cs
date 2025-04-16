using FluentValidation;
using MassTransit;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Messaging.Key;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;

namespace MessagingSystem.Services.User.Application;

public static class AddApplication
{
    public static void AddApplicationLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<RegisterOrchestrator>();
        services.AddScoped<LoginOrchestrator>(); 
        services.AddScoped<UserOrchestrator>();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>(); 
        services.AddScoped<IValidator<UserDto>, UserValidator>();
        
        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.AddConsumer<PublicKeyConsumer>();
    
            busConfiguration.UsingRabbitMq((context, configurator) =>
            {
                var settings = context.GetRequiredService<MessageBrokerSettings>();
         
                configurator.Host(new Uri(settings.Host), h =>
                {
                    h.Username(settings.UserName);
                    h.Password(settings.Password);
                });
        
                configurator.ReceiveEndpoint("public-key-notification-queue", e =>
                {
                    e.ConfigureConsumer<PublicKeyConsumer>(context);
                });
            });
        });

    }
}