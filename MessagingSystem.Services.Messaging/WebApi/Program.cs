using System.Text;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.MessageBroker.Key;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Validators;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using MessagingSystem.Services.Messaging.Persistence;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.WebApi.Messages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OtoAppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddScoped<IMessageOrchestrator, MessageOrchestrator>();
builder.Services.AddScoped<IChatOrchestrator, ChatOrchestrator>();
builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();
builder.Services.AddScoped<IValidator<MessagesDto>, MessageCreateValidator>();
builder.Services.AddScoped<IValidator<EditMessageDto>, MessageEditValidator>();

builder.Services.AddScoped<IHasher, Hasher>();
builder.Services.AddScoped<IEncryptionInfo, EncryptionInfo>();
builder.Services.AddScoped<IDecryptionInfo, DecryptionInfo>();
builder.Services.AddSingleton<IPublicKeyStorage, PublicKeyStorage>();
builder.Services.AddScoped<KeyPublisher>();

builder.Services.AddAutoMapper(config => config.AddProfile(new MessageMap()));
builder.Services.AddSignalR();

builder.Services.Configure<MessageBrokerSettings>(builder.Configuration.GetSection("MessageBroker"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

builder.Services.AddMassTransit(busConfiguration =>
{
    busConfiguration.AddRequestClient<ExistingUserRequest>(new Uri("queue:existing-user-request"));
    busConfiguration.AddConsumer<PublicKeyConsumer>();
    
    busConfiguration.UsingRabbitMq((context, configurator) =>
    {
        var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;
        configurator.Host(new Uri(settings.Host), h =>
        {
            h.Username(settings.UserName);
            h.Password(settings.Password);
        });
        configurator.ReceiveEndpoint("public-key-user-service-queue", e =>
        {
            e.ConfigureConsumer<PublicKeyConsumer>(context);
        });
    });
});

builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["JWTConfig:Issuer"],
                ValidAudience = builder.Configuration["JWTConfig:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTConfig:Key"] ?? string.Empty)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
        
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (string.IsNullOrEmpty(accessToken) && context.Request.Cookies.TryGetValue("access_token", out var encryptedToken))
                    {
                        var decryptService = context.HttpContext.RequestServices.GetRequiredService<IDecryptionInfo>();
                        accessToken = decryptService.Decrypt(encryptedToken);
                    }

                    if (string.IsNullOrEmpty(accessToken)) 
                        return Task.CompletedTask;

                    context.Token = accessToken;
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddSwaggerGen(options =>
        {
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Description = "Enter your JWT access token",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
        });
        
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:63342")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("access_token", out var encryptedToken))
    {
        var decryptService = context.RequestServices.GetRequiredService<IDecryptionInfo>();
        var accessToken = decryptService.Decrypt(encryptedToken);
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }
    }
    
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var publisher = scope.ServiceProvider.GetRequiredService<KeyPublisher>();
    await publisher.PublishAsync();
}

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHub<OtoChatHub>("/chatHub").RequireAuthorization();

app.Run();