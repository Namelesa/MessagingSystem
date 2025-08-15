using System.Text;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.Services.Messaging.Application.MessageBroker.AddUser;
using MessagingSystem.Services.Messaging.Application.MessageBroker.Key;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;
using MessagingSystem.Services.Messaging.Infrastructure.Caching;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.ImageLoader;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MessagingSystem.Services.Messaging.Infrastructure;

public static class AddInfrastructure
{
    public static void AddInfrastructureLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddScoped<IHasher, Hasher.Hasher>();
        services.AddScoped<IEncryptionInfo, EncryptionInfo>();
        services.AddScoped<IDecryptionInfo, DecryptionInfo>();
        services.AddSingleton<IPublicKeyStorage, PublicKeyStorage>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IImageLoaderService, ImageLoaderService>();
        services.AddScoped<KeyPublisher>();
        
        services.Configure<MessageBrokerSettings>(configuration.GetSection("MessageBroker"));

        services.Configure<DigitalOceanSpacesSettings>(
            configuration.GetSection("DigitalOceanSpacesSettings"));
        
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<DigitalOceanSpacesSettings>>().Value);
        
    services.AddSingleton(sp =>
        sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

    services.AddMassTransit(busConfiguration =>
    {
        busConfiguration.AddRequestClient<EditUserInfoRequest>(new Uri("queue:edit-user-request"));
        busConfiguration.AddRequestClient<DeleteUserInfoRequest>(new Uri("queue:delete-user-request"));

        busConfiguration.AddConsumer<PublicKeyConsumer>();
        busConfiguration.AddConsumer<EditUserInfoConsumer>();
        busConfiguration.AddConsumer<DeleteUserInfoConsumer>();
        busConfiguration.AddConsumer<AddUserConsumer>();
        
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
            configurator.ReceiveEndpoint("edit-user-request", e =>
            {
                e.ConfigureConsumer<EditUserInfoConsumer>(context);
            });
            configurator.ReceiveEndpoint("delete-user-request", e =>
            {
                e.ConfigureConsumer<DeleteUserInfoConsumer>(context);
            });
            configurator.ReceiveEndpoint("add-user-request", e =>
            {
                e.ConfigureConsumer<AddUserConsumer>(context);
            });
        });
    });

    services.AddAuthentication(options =>
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
                ValidIssuer = configuration["JWTConfig:Issuer"],
                ValidAudience = configuration["JWTConfig:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWTConfig:Key"] ?? string.Empty)),
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
        services.AddSwaggerGen(options =>
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
        
        services.AddAuthorization();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:4200",
                        "http://localhost:63342")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration
                .GetRequiredSection("Redis").GetValue<string>("Host");
        });
    }
}