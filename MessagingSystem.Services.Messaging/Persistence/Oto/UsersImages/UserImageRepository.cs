using MessagingSystem.Services.Messaging.Core.Oto.Users;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;

public class UserImageRepository(OtoAppDbContext db) : IUserImageRepository
{
    public async Task<UserImage?> FindUserImageByHashAsync(string nicknameHash) =>
        await db.Images.FindAsync(nicknameHash);
    public async Task<UserImage> AddUserImageAsync(UserImage usersImages)
    {
        try
        {
            await db.Images.AddAsync(usersImages);
            await db.SaveChangesAsync();
            return usersImages;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    public async Task<UserImage> EditUserImageAsync(UserImage usersImages)
    {
        db.Images.Update(usersImages);
        await db.SaveChangesAsync();
        return usersImages;
    }
    public async Task<UserImage> DeleteUserImageAsync(UserImage usersImages)
    {
        db.Images.Remove(usersImages);
        await db.SaveChangesAsync();
        return usersImages;
    }
}