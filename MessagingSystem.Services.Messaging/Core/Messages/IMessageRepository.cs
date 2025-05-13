namespace MessagingSystem.Services.Messaging.Core.Messages;

public interface IMessageRepository
{
    Task<List<Message>> FindMessageByTimeAsync(DateTime dateTime);
    Task<List<Message>> FindMessagesByContentAsync(string pathContent);
    Task<List<Message>> FindMessageByRecipientAsync(string recipientName);
    Task<List<Message>> FindMessageBySenderAsync(string senderName);
    Task<Message?> FindMessageByIdAsync(Guid id);
    Task<Message?> CreateMessageAsync(Message message);
    Task<Message> EditMessageAsync(Message message);
    Task<Message> DeleteMessageAsync(Message message);
    Task<Message> SoftDeleteMessageAsync(Message message);
    Task<List<Message>> GetMessageStoryAsync(string sender, string recipient, int take);
}