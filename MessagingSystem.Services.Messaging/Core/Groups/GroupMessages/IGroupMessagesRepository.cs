using MessagingSystem.Services.Messaging.Core.Messages;

namespace MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;

public interface IGroupMessagesRepository : IMessageRepository<GroupMessage>
{
    Task<List<GroupMessage>> GetMessageStoryAsync(Guid groupId, int take);
}