using MessagingSystem.Services.Notification.Application;
using MessagingSystem.Services.Notification.Infrastructure;
using MessagingSystem.Services.Notification.Infrastructure.Key;
using MessagingSystem.Services.Notification.Persistence;

namespace MessagingSystem.Services.Notification.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPersistenceLayer(builder.Configuration);
        builder.Services.AddInfrastructureLayer(builder.Configuration);
        builder.Services.AddApplicationLayer(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}