namespace MessagingSystem.Services.Messaging.Core.Oto.Users;

public class UserImage(string nickNameHash, string image)
{
    public Guid Id { get; init; }
    public string NickNameHash { get; private set; } = nickNameHash;
    public string Image { get; private set; } = image;
    
    public void EditInfo(string nickNameHash, string image)
    {
        NickNameHash = nickNameHash;
        Image = image;
    }
}