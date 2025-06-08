namespace MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;

public interface IGroupMessagesRepository
{
    Task<List<GroupMessage>?> FindMessagesAsync(MessageFilter filter);
    Task<GroupMessage> ReplyMessageAsync(Guid replyId, GroupMessage message);
    Task<GroupMessage?> FindMessageByIdAsync(Guid id);
    Task<GroupMessage?> CreateMessageAsync(GroupMessage message);
    Task<GroupMessage> EditMessageAsync(GroupMessage message);
    Task<GroupMessage> DeleteMessageAsync(GroupMessage message);
    Task<GroupMessage> SoftDeleteMessageAsync(GroupMessage message);
    Task<List<GroupMessage>> GetMessageStoryAsync(Guid groupId, int take);
    Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash);
    Task<int> DeleteUserHashesAsync(string userHash);
}