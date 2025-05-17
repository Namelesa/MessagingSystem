namespace MessagingSystem.Services.Messaging.Core.Oto.OtoChats;

public interface IChatRepository
{
    Task<List<string>?> GetChatsAsync(string currentUserName);
}