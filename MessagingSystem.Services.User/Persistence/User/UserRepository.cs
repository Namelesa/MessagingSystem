using MessagingSystem.Services.User.Core.User;

namespace MessagingSystem.Services.User.Persistence.User;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<Core.User.User?> FindUserByNickNameAsync(string nickName) => await db.Users.FindAsync(nickName);

    public async Task<Core.User.User?> FindUserByIdAsync(Guid id) => await db.Users.FindAsync(id);

    public async Task AddUserAsync(Core.User.User user)
    {
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(Core.User.User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Core.User.User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}