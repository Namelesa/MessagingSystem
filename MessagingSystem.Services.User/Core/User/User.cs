using Microsoft.AspNetCore.Identity;

namespace MessagingSystem.Services.User.Core.User;

public class User(string login, string nickName, string? image) : IdentityUser
{
    public string Login { get; private set; } = login;
    public string NickName { get; private set; } = nickName;

    public string? HashLogin { get; private set; }
    public string? HashEmail { get; private set; }
    public string? HashNickName { get; private set; }
    public string? Image { get; private set; } = image;
    
    public void SetHashes(string loginHash, string emailHash, string nickNameHash)
    {
        HashLogin = loginHash;
        HashEmail = emailHash;
        HashNickName = nickNameHash;
    }
    
    public void SetImage(string? image)
    {
        Image = image;
    }
}