using System.Text;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MessagingSystem.Services.User.Infrastructure;

public static class AddInfrastructure
{
    public static void AddInfrastructureLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IHasher, Hasher>();
        services.AddScoped<IHasherPassword, HasherPassword>();
        services.AddSingleton<IEncryptionInfo, EncryptionInfo>();
        services.AddSingleton<IDecryptionInfo, DecryptionInfo>();
        services.AddSingleton<IPublicKeyStorage, PublicKeyStorage>();
        services.AddScoped<KeyPublisher>();
        services.Configure<MessageBrokerSettings>(
            configuration.GetSection("MessageBroker"));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);
        
        services.AddScoped<IJwtService, JwtService>();
        
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
                    if (!context.Request.Cookies.ContainsKey("access_token")) 
                        return Task.CompletedTask;
                    var encryptedToken = context.Request.Cookies["access_token"];
                    var decryptService = context.HttpContext.RequestServices.GetRequiredService<IDecryptionInfo>();
                    if (encryptedToken == null) 
                        return Task.CompletedTask;
                    var decryptedToken = decryptService.Decrypt(encryptedToken);
                    context.Token = decryptedToken;

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
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {jwtSecurityScheme, Array.Empty<string>()}
            });
        });
        
        services.AddAuthorization();
    }
}