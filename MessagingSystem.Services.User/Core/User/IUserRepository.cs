namespace MessagingSystem.Services.User.Core.User;

public interface IUserRepository
{
    Task<User?> FindUserByHashLoginAsync(string login);
    Task<User?> FindUserByHashNickNameAsync(string hashNickName);
    Task<User?> FindUserByIdAsync(string id);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(User user);
}