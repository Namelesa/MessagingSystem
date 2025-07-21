using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Add;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Application.Messaging.Key;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;
using Microsoft.Extensions.Options;

namespace MessagingSystem.Services.User.Application;

public static class AddApplication
{
    public static void AddApplicationLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MessageBrokerSettings>(configuration.GetSection("MessageBrokerSettings"));
        
        services.AddScoped<IRegisterOrchestrator, RegisterOrchestrator>();
        services.AddScoped<ILoginOrchestrator, LoginOrchestrator>(); 
        services.AddScoped<IUserOrchestrator, UserOrchestrator>();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>(); 
        services.AddScoped<IValidator<UserDto>, UserValidator>();
        
        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.AddRequestClient<AddUserRequest>(new Uri("queue:add-user-request"));
            busConfiguration.AddConsumer<PublicKeyConsumer>();
    
            busConfiguration.UsingRabbitMq((context, configurator) =>
            {
                var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;
         
                configurator.Host(new Uri(settings.Host), h =>
                {
                    h.Username(settings.UserName);
                    h.Password(settings.Password);
                });
        
                configurator.ReceiveEndpoint("public-key-notification-queue", e =>
                {
                    e.ConfigureConsumer<PublicKeyConsumer>(context);
                });
                
                configurator.ReceiveEndpoint("public-key-messaging-queue", e =>
                {
                    e.ConfigureConsumer<PublicKeyConsumer>(context);
                });
            });
        });
    }
}