using MessagingSystem.Services.Messaging.Core.Messages;

namespace MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;

public interface IOtoMessageRepository : IMessageRepository<Message>
{
    Task<List<Message>> GetMessageStoryAsync(string sender, string recipient, int take);
}