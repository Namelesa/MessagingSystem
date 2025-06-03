using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;

public class OtoDbInitializer(OtoAppDbContext db) : IOtoDbInitializer
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