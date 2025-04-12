using Microsoft.AspNetCore.Identity;

namespace MessagingSystem.Services.User.Core.User;

public class User(string login, string nickName) : IdentityUser
{
    public string Login { get; private set; } = login;
    public string NickName { get; private set; } = nickName;
    
}