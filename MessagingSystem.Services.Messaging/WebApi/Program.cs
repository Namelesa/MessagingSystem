using System.Text;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.User;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Validators;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using MessagingSystem.Services.Messaging.Application.MessageBroker.Key;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Validators;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;
using MessagingSystem.Services.Messaging.WebApi.Group.Info;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages.Chat;
using MessagingSystem.Services.Messaging.WebApi.Messages.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OtoAppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<GroupAppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("GroupDefaultConnection")));

builder.Services.AddSingleton<IDbContextFactory<GroupAppDbContext>>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("GroupDefaultConnection");
    var optionsBuilder = new DbContextOptionsBuilder<GroupAppDbContext>();
    optionsBuilder.UseNpgsql(connectionString);
    return new PooledDbContextFactory<GroupAppDbContext>(optionsBuilder.Options);
});


builder.Services.AddScoped<IOtoMessageRepository, OtoMessageRepository>();
builder.Services.AddScoped<IGroupInfoRepository, GroupInfoRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IGroupMembersRepository, GroupMemberRepository>();
builder.Services.AddScoped<IGroupMessagesRepository, GroupMessagesRepository>();
builder.Services.AddScoped<IUserImageRepository, UserImageRepository>();
builder.Services.AddScoped<IOtoDbInitializer, OtoDbInitializer>();
builder.Services.AddScoped<IGroupDbInitializer, GroupDbInitializer>();

builder.Services.AddScoped<IMessageOrchestrator, MessageOrchestrator>();
builder.Services.AddScoped<IChatOrchestrator, ChatOrchestrator>();
builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();
builder.Services.AddScoped<IGroupInfoOrchestrator, GroupInfoOrchestrator>();
builder.Services.AddScoped<IGroupMemberOrchestrator, GroupMemberOrchestrator>();
builder.Services.AddScoped<IGroupMessagesOrchestrator, GroupMessagesOrchestrator>();
builder.Services.AddScoped<IValidator<MessagesDto>, MessageCreateValidator>();
builder.Services.AddScoped<IValidator<EditMessageDto>, MessageEditValidator>();
builder.Services.AddScoped<IValidator<GroupDto>, GroupDtoValidator>();
builder.Services.AddScoped<IValidator<EditGroupDto>, EditGroupDtoValidator>();
builder.Services.AddScoped<IValidator<GroupMembersDto>, GroupMembersValidator>();
builder.Services.AddScoped<IValidator<GroupMessageDto>, GroupMessageCreateValidator>();
builder.Services.AddScoped<IGroupEncryption, GroupEncryptionDecorator>();

builder.Services.AddScoped<IHasher, Hasher>();
builder.Services.AddScoped<IEncryptionInfo, EncryptionInfo>();
builder.Services.AddScoped<IDecryptionInfo, DecryptionInfo>();
builder.Services.AddSingleton<IPublicKeyStorage, PublicKeyStorage>();
builder.Services.AddScoped<KeyPublisher>();

builder.Services.AddAutoMapper(config => config.AddProfile(new MessageMap()));
builder.Services.AddAutoMapper(config => config.AddProfile(new GroupMap()));
builder.Services.AddAutoMapper(config => config.AddProfile(new GroupMessagesMapper()));
builder.Services.AddAutoMapper(config => config.AddProfile(new UserMap()));
builder.Services.AddAutoMapper(config => config.AddProfile(new ChatMap()));
builder.Services.AddSignalR();

builder.Services.Configure<MessageBrokerSettings>(builder.Configuration.GetSection("MessageBroker"));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

builder.Services.AddMassTransit(busConfiguration =>
{
    busConfiguration.AddRequestClient<ExistingUserRequest>(new Uri("queue:existing-user-request"));
    busConfiguration.AddRequestClient<ExistingUsersRequest>(new Uri("queue:existing-users-request"));
    busConfiguration.AddRequestClient<EditUserInfoRequest>(new Uri("queue:edit-user-request"));
    busConfiguration.AddRequestClient<DeleteUserInfoRequest>(new Uri("queue:delete-user-request"));

    busConfiguration.AddConsumer<PublicKeyConsumer>();
    busConfiguration.AddConsumer<EditUserInfoConsumer>();
    busConfiguration.AddConsumer<DeleteUserInfoConsumer>();


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
    var otoDbInitializer = scope.ServiceProvider.GetRequiredService<IOtoDbInitializer>();
    var groupDbInitializer = scope.ServiceProvider.GetRequiredService<IGroupDbInitializer>();
    await publisher.PublishAsync();
    await otoDbInitializer.Initialize();
    await groupDbInitializer.Initialize();
}

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHub<OtoChatHub>("/otoChatHub").RequireAuthorization();
app.MapHub<GroupChatHub>("/groupChatHub").RequireAuthorization();

app.Run();