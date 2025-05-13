using MessagingSystem.Services.Messaging.Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Messages;

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task<List<Message>> FindMessageByTimeAsync(DateTime dateTime)
    {
        var messages = db.UsersMessages
            .Where(u => u.Date == dateTime || u.EditDate == dateTime);

        return await messages.ToListAsync();
    }

    public async Task<List<Message>> FindMessagesByContentAsync(string searchContent)
    {
        var messages = db.UsersMessages
            .Where(m => m.Content.Contains(searchContent));

        return await messages.ToListAsync();
    }

    public async Task<List<Message>> FindMessageByRecipientAsync(string recipientName)
    {
        var messages = db.UsersMessages
            .Where(m => m.Recipient == recipientName);

        return await messages.ToListAsync();
    }

    public async Task<List<Message>> FindMessageBySenderAsync(string senderName)
    {
        var messages = db.UsersMessages
            .Where(m => m.Sender == senderName);

        return await messages.ToListAsync();
    }

    public async Task<Message?> FindMessageByIdAsync(Guid id) =>
        await db.UsersMessages.FirstOrDefaultAsync(u => u.Id == id); 

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