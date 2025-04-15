using FluentValidation;
using MassTransit;
using MessagingSystem.Services.Notification.Application.Messaging;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Infrastructure.Encrypt;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using MessagingSystem.Services.Notification.Infrastructure.MessageBroker;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using MessagingSystem.Services.Notification.Persistence;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ITemplateReader, TemplateReader>();
builder.Services.AddScoped<IEncryptInfo, EncryptInfo>();
builder.Services.AddScoped<IValidator<UserDto>, UserValidator>();
builder.Services.AddTransient<INotification, Notification>();

builder.Services.AddScoped<NotificationOrchestrator>();

builder.Services.Configure<MessageBrokerSettings>(
    builder.Configuration.GetSection("MessageBroker"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

builder.Services.AddMassTransit(busConfiguration =>
{
    busConfiguration.AddConsumer<ConfirmEmailConsumer>();
    busConfiguration.AddConsumer<EditUserInfoConsumer>();
    busConfiguration.AddConsumer<DeleteUserInfoConsumer>();
    
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
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();