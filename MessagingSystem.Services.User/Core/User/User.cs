using Microsoft.AspNetCore.Identity;

namespace MessagingSystem.Services.User.Core.User;

public class User(string login, string nickName) : IdentityUser
{
    public string Login { get; private set; } = login;
    public string NickName { get; private set; } = nickName;

    public string? HashLogin { get; private set; }
    public string? HashEmail { get; private set; }
    public string? HashNickName { get; private set; }
    
    public void SetHashes(string loginHash, string emailHash, string nickNameHash)
    {
        HashLogin = loginHash;
        HashEmail = emailHash;
        HashNickName = nickNameHash;
    }

}