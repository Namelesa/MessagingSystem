using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();
builder.Services.AddScoped<IHasher, Hasher>();
builder.Services.AddScoped<IEncryptionInfo, EncryptionInfo>();
builder.Services.AddScoped<IDecryptionInfo, DecryptionInfo>();
builder.Services.AddSignalR();

builder.Services.Configure<MessageBrokerSettings>(builder.Configuration.GetSection("MessageBroker"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

builder.Services.AddMassTransit(x =>
{
    x.AddRequestClient<ExistingUserRequest>(new Uri("queue:existing-user-request"));

    x.UsingRabbitMq((context, cfg) =>
    {
        var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;
        cfg.Host(new Uri(settings.Host), h =>
        {
            h.Username(settings.UserName);
            h.Password(settings.Password);
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();