namespace MessagingSystem.Services.Messaging.Core.Messages;

public interface IMessageRepository<T> where T : class
{
    Task<List<T>?> FindMessagesAsync(MessageFilter filter);
    Task<T> ReplyMessageAsync(Guid replyId, T message);
    Task<T?> FindMessageByIdAsync(Guid id);
    Task<T?> CreateMessageAsync(T message);
    Task<T> EditMessageAsync(T message);
    Task<T> DeleteMessageAsync(T message);
    Task<T> SoftDeleteMessageAsync(T message);
    Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash);
    Task<int> DeleteUserHashesAsync(string userHash);
}