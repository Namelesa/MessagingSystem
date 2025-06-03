using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;

public class GroupGroupDbInitializer(GroupAppDbContext db) : IGroupDbInitializer
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
            Console.WriteLine("Can not do migration");
        }
    }
}