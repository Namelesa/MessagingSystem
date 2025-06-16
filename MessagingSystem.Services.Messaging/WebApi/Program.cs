using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Infrastructure;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using MessagingSystem.Services.Messaging.Persistence;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;
using MessagingSystem.Services.Messaging.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebApiLayer(builder.Configuration);
builder.Services.AddPersistenceLayer(builder.Configuration);
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddApplicationLayer(builder.Configuration);

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