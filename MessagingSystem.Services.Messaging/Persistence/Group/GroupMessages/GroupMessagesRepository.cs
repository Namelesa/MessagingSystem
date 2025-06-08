using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;

public class GroupMessagesRepository(GroupAppDbContext db) : IGroupMessagesRepository
{
    public async Task<List<GroupMessage>?> FindMessagesAsync(MessageFilter filter)
    {
        return await db.GroupMessages
            .Where(m =>
                (filter.Sender == null || m.SenderHash == filter.Sender) &&
                (filter.Date == null || m.SendTime.Date == filter.Date.Value.Date)
            )
            .ToListAsync();
    }
    public async Task<GroupMessage> ReplyMessageAsync(Guid replyId, GroupMessage message)
    {
        message.Reply(replyId);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<GroupMessage?> FindMessageByIdAsync(Guid id) =>
        await db.GroupMessages.FirstOrDefaultAsync(u => u.Id == id);
    public async Task<GroupMessage?> CreateMessageAsync(GroupMessage message)
    {
        await db.AddAsync(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<GroupMessage> EditMessageAsync(GroupMessage message)
    {
        db.GroupMessages.Update(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<GroupMessage> DeleteMessageAsync(GroupMessage message)
    {
        db.GroupMessages.Remove(message);
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<GroupMessage> SoftDeleteMessageAsync(GroupMessage message)
    {
        message.SoftDeleteInfo();
        await db.SaveChangesAsync();
        return message;
    }
    public async Task<List<GroupMessage>> GetMessageStoryAsync(Guid groupId, int take)=> 
        await db.GroupMessages.Where(u=> u.GroupId == groupId)
            .OrderByDescending(u => u.SendTime)
            .Take(take)
            .ToListAsync();
    public async Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash)
    {
        var senderUpdated = await db.Database.ExecuteSqlRawAsync(@"
        UPDATE ""GroupMessages""
        SET ""Sender"" = {0}, ""SenderHash"" = {1}
        WHERE ""SenderHash"" = {2}",
            newNick, newHash, oldHash);
        
        return senderUpdated;
    }
    public async Task<int> DeleteUserHashesAsync(string userHash)
    {
        var deleted = await db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM ""GroupMessages""
        WHERE ""SenderHash"" = {0}",userHash);

        return deleted;
    }
}