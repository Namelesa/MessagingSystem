namespace MessagingSystem.Services.Messaging.Application.Chats;

public interface IChatOrchestrator
{
    Task<List<string>> GetChatsAsync(string currentUserName);
}