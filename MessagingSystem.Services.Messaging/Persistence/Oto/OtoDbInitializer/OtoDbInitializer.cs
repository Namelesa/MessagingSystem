using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;

public class OtoDbInitializer(OtoAppDbContext db, ILogger<OtoDbInitializer> logger) : IOtoDbInitializer
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