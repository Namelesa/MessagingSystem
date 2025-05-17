using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group;

public class GroupAppDbContext(DbContextOptions<GroupAppDbContext> options) : DbContext(options)
{
    
}