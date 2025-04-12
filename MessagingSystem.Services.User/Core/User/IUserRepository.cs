namespace MessagingSystem.Services.User.Core.User;

public interface IUserRepository
{
    Task<User?> FindUserByNickNameAsync(string nickName);
    Task<User?> FindUserByIdAsync(Guid id);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(User user);
}