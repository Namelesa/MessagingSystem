namespace MessagingSystem.Services.Messaging.Core.Oto.Users;

public interface IUserImageRepository
{
    Task<UserImage?> FindUserImageByHashAsync(string nicknameHash);
    Task<UserImage> AddUserImageAsync(UserImage usersImages);
    Task<UserImage> EditUserImageAsync(UserImage usersImages);
    Task<UserImage> DeleteUserImageAsync(UserImage usersImages);
}