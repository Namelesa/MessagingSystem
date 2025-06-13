namespace MessagingSystem.Services.Messaging.Core.Oto.OtoChats;

public interface IChatRepository
{
    Task<List<Chat>?> GetChatsAsync(string currentUserName);
}