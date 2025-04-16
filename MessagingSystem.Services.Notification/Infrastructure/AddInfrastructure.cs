using MassTransit;
using MessagingSystem.Services.Notification.Application.Messaging.Email;
using MessagingSystem.Services.Notification.Application.Messaging.Key;
using MessagingSystem.Services.Notification.Infrastructure.Encrypt;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using MessagingSystem.Services.Notification.Infrastructure.MessageBroker;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace MessagingSystem.Services.Notification.Infrastructure;

public static class AddInfrastructure
{
    public static void AddInfrastructureLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<KeyPublisher.KeyPublisher>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<ITemplateReader, TemplateReader>();
        services.AddScoped<IEncryptInfo, EncryptInfo>();
        
        services.Configure<MessageBrokerSettings>(
            configuration.GetSection("MessageBroker"));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.AddConsumer<ConfirmEmailConsumer>();
            busConfiguration.AddConsumer<EditUserInfoConsumer>();
            busConfiguration.AddConsumer<DeleteUserInfoConsumer>();
            busConfiguration.AddConsumer<PublicKeyConsumer>();
    
            busConfiguration.UsingRabbitMq((context, configurator) =>
            {
                var settings = context.GetRequiredService<MessageBrokerSettings>();
         
                configurator.Host(new Uri(settings.Host), h =>
                {
                    h.Username(settings.UserName);
                    h.Password(settings.Password);
                });
        
                configurator.ReceiveEndpoint("confirm-email-queue", e =>
                {
                    e.ConfigureConsumer<ConfirmEmailConsumer>(context);
                });
                configurator.ReceiveEndpoint("edit-user-queue", e =>
                {
                    e.ConfigureConsumer<EditUserInfoConsumer>(context);
                });
                configurator.ReceiveEndpoint("delete-user-queue", e =>
                {
                    e.ConfigureConsumer<DeleteUserInfoConsumer>(context);
                });
                configurator.ReceiveEndpoint("public-key-user-queue", e =>
                {
                    e.ConfigureConsumer<PublicKeyConsumer>(context);
                });
            });
        });
    }
}