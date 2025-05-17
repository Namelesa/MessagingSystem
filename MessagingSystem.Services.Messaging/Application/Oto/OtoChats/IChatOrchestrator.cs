namespace MessagingSystem.Services.Messaging.Application.Oto.OtoChats;

public interface IChatOrchestrator
{
    Task<List<string>?> GetChatsAsync(string currentUserName);
}