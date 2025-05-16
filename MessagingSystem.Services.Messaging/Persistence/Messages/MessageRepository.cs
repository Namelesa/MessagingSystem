using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Messages;

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task<Message?> FindMessageByIdAsync(Guid id) =>
        await db.UsersMessages.FirstOrDefaultAsync(u => u.Id == id);
    public async Task<List<Message>?> FindMessagesAsync(MessageFilter filter)
    {
        return await db.UsersMessages
            .Where(m =>
                (filter.Sender == null || m.Sender == filter.Sender) &&
                (filter.Recipient == null || m.Recipient == filter.Recipient) &&
                (filter.Date == null || m.Date.Date == filter.Date.Value.Date) &&
                (filter.Content == null || EF.Functions.ILike(m.Content, $"%{filter.Content}%"))
            )
            .ToListAsync();
    }
    public async Task<Message> ReplyMessageAsync(Guid replyId, Message message)
    {
        message.Reply(replyId);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<Message?> CreateMessageAsync(Message message)
    {
        await db.AddAsync(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<Message> EditMessageAsync(Message message)
    {
        db.UsersMessages.Update(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<Message> DeleteMessageAsync(Message message)
    {
        db.UsersMessages.Remove(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<Message> SoftDeleteMessageAsync(Message message)
    {
        message.SoftDeleteInfo();
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<List<Message>> GetMessageStoryAsync(string sender, string recipient, int take)
    {
        return await db.UsersMessages
            .Where(m =>
                (m.Sender == sender && m.Recipient == recipient) ||
                (m.Sender == recipient && m.Recipient == sender))
            .OrderByDescending(m => m.Date)
            .Take(take)
            .ToListAsync();
    }
}