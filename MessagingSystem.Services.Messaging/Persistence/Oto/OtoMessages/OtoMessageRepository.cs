using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;

public class OtoMessageRepository(OtoAppDbContext db) 
    : MessageRepositoryBase<Message>(db), IOtoMessageRepository
{
    public override async Task<Message?> FindMessageByIdAsync(Guid id) => 
        await db.UsersMessages.FirstOrDefaultAsync(u => u.Id == id);
    public override async Task<Message?> CreateMessageAsync(Message message)
    {
        await db.AddAsync(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<Message> EditMessageAsync(Message message)
    {
        db.UsersMessages.Update(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<Message> DeleteMessageAsync(Message message)
    {
        db.UsersMessages.Remove(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<Message> SoftDeleteMessageAsync(Message message)
    {
        message.SoftDeleteInfo();
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash)
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
    public override async Task<int> DeleteUserHashesAsync(string userHash)
    {
        var deleted = await db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM ""UsersMessages""
        WHERE ""SenderHash"" = {0} OR ""RecipientHash"" = {0}", userHash);

        return deleted;
    }
    public async Task<List<Message>> GetMessageStoryAsync(string sender, string recipient, int skip, int take)
    {
        return await db.UsersMessages
            .Where(m =>
                (m.SenderHash == sender && m.RecipientHash == recipient) ||
                (m.SenderHash == recipient && m.RecipientHash == sender))
            .OrderByDescending(m => m.SendTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    public async Task<List<Message>> FindMessagesByHashAsync(string userHash)
    {
        return await db.UsersMessages
            .Where(m => m.SenderHash == userHash || m.RecipientHash == userHash)
            .ToListAsync();
    }
    public override async Task<Message> ReplyMessageAsync(Guid replyId, Message message)
    {
        message.Reply(replyId);
        await db.SaveChangesAsync();
        return message;
    }
}