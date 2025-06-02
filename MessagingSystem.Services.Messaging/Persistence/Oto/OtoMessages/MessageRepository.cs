using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;

public class MessageRepository(OtoAppDbContext db) : IMessageRepository
{
    public async Task<Message?> FindMessageByIdAsync(Guid id) =>
        await db.UsersMessages.FirstOrDefaultAsync(u => u.Id == id);
    public async Task<List<Message>?> FindMessagesAsync(MessageFilter filter)
    {
        return await db.UsersMessages
            .Where(m =>
                (filter.Sender == null || m.SenderHash == filter.Sender) &&
                (filter.Recipient == null || m.RecipientHash == filter.Recipient) &&
                (filter.Date == null || m.Date.Date == filter.Date.Value.Date)
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
                (m.SenderHash == sender && m.RecipientHash == recipient) ||
                (m.SenderHash == recipient && m.RecipientHash == sender))
            .OrderByDescending(m => m.Date)
            .Take(take)
            .ToListAsync();
    }
    public async Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash)
    {
        var senderUpdated = await db.Database.ExecuteSqlRawAsync(@"
        UPDATE ""UsersMessages""
        SET ""Sender"" = {0}, ""SenderHash"" = {1}
        WHERE ""SenderHash"" = {2}",
            newNick, newHash, oldHash);

        var recipientUpdated = await db.Database.ExecuteSqlRawAsync(@"
        UPDATE ""UsersMessages""
        SET ""Recipient"" = {0}, ""RecipientHash"" = {1}
        WHERE ""RecipientHash"" = {2}",
            newNick, newHash, oldHash);
        
        return senderUpdated + recipientUpdated;
    }

    public async Task<int> DeleteUserHashesAsync(string userHash)
    {
        var deleted = await db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM ""UsersMessages""
        WHERE ""SenderHash"" = {0} OR ""RecipientHash"" = {0}", userHash);

        return deleted;
    }
}