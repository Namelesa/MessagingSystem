using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoChats;

public interface IChatOrchestrator
{
    Task<List<ChatDto>?> GetChatsAsync(string currentUserName);
}