using MessagingSystem.Services.Messaging.Core.Oto.Users;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;

public class UserImageRepository(IDbContextFactory<OtoAppDbContext> db) : IUserImageRepository
{
    private async Task<TResult> WithContextAsync<TResult>(Func<OtoAppDbContext, Task<TResult>> action)
    {
        await using var context = await db.CreateDbContextAsync();
        return await action(context);
    }

    public async Task<UserImage?> FindUserImageByHashAsync(string nicknameHash) =>
        await WithContextAsync(context => 
            context.Images.FirstOrDefaultAsync(u => u.NickNameHash == nicknameHash));
    public async Task<UserImage> AddUserImageAsync(UserImage usersImages)
    {
        try
        {
            return await WithContextAsync(async context =>
            {
                await context.Images.AddAsync(usersImages);
                await context.SaveChangesAsync();
                return usersImages;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public async Task<UserImage> EditUserImageAsync(UserImage usersImages)
    {
        return await WithContextAsync(async context =>
        {
            context.Images.Update(usersImages);
            await context.SaveChangesAsync();
            return usersImages;
        });
    }
    public async Task<UserImage> DeleteUserImageAsync(UserImage usersImages)
    {
        return await WithContextAsync(async context =>
        {
            context.Images.Remove(usersImages);
            await context.SaveChangesAsync();
            return usersImages;
        });
    }
}