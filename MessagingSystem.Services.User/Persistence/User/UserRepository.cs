using MessagingSystem.Services.User.Core.User;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.User.Persistence.User;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<Core.User.User?> FindUserByHashLoginAsync(string login) => 
        await db.Users.FirstOrDefaultAsync(u => u.HashLogin == login);
    
    public async Task<Core.User.User?> FindUserByHashNickNameAsync(string hashNickName) => 
        await db.Users.FirstOrDefaultAsync(u => u.HashNickName == hashNickName);
    
    public async Task<List<Core.User.User>?> FindUsersByHashNickNamesAsync(List<string> hashNickNames) => 
         await db.Users
        .Where(u => u.HashNickName != null && hashNickNames.Contains(u.HashNickName))
        .ToListAsync();
    
    public async Task<Core.User.User?> FindUserByIdAsync(string id) => 
        await db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task AddUserAsync(Core.User.User user)
    {
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }
    
    public async Task UpdateUserAsync(Core.User.User user)
    {
        user.EmailConfirmed = true;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Core.User.User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }
}