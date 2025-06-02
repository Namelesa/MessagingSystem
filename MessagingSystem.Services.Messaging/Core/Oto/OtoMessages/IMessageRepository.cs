namespace MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;

public interface IMessageRepository
{
    Task<List<Message>?> FindMessagesAsync(MessageFilter filter);
    Task<Message> ReplyMessageAsync(Guid replyId, Message message);
    Task<Message?> FindMessageByIdAsync(Guid id);
    Task<Message?> CreateMessageAsync(Message message);
    Task<Message> EditMessageAsync(Message message);
    Task<Message> DeleteMessageAsync(Message message);
    Task<Message> SoftDeleteMessageAsync(Message message);
    Task<List<Message>> GetMessageStoryAsync(string sender, string recipient, int take);
    Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash);
    Task<int> DeleteUserHashesAsync(string userHash);
}