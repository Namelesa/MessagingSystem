using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;

public class GroupDbInitializer(GroupAppDbContext db, ILogger<GroupDbInitializer> logger) : IGroupDbInitializer
{
    public async Task Initialize()
    {
        try
        {
            if ((await db.Database.GetPendingMigrationsAsync()).Any())
            {
                await db.Database.MigrateAsync();
            }
        }
        catch
        {
            logger.LogInformation("Error with migration");
        }
    }
}