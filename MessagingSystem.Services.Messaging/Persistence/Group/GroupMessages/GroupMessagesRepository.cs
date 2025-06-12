using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Persistence.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;

public class GroupMessagesRepository(GroupAppDbContext db) 
    : MessageRepositoryBase<GroupMessage>(db), IGroupMessagesRepository
{
    public override async Task<GroupMessage?> FindMessageByIdAsync(Guid id) =>
        await db.GroupMessages.FirstOrDefaultAsync(u => u.Id == id);
    public override async Task<GroupMessage?> CreateMessageAsync(GroupMessage message)
    {
        await db.AddAsync(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<GroupMessage> EditMessageAsync(GroupMessage message)
    {
        db.GroupMessages.Update(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<GroupMessage> DeleteMessageAsync(GroupMessage message)
    {
        db.GroupMessages.Remove(message);
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<GroupMessage> SoftDeleteMessageAsync(GroupMessage message)
    {
        message.SoftDeleteInfo();
        await db.SaveChangesAsync();
        return message;
    }
    public override async Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash)
    {
        var senderUpdated = await db.Database.ExecuteSqlRawAsync(@"
        UPDATE ""GroupMessages""
        SET ""Sender"" = {0}, ""SenderHash"" = {1}
        WHERE ""SenderHash"" = {2}",
            newNick, newHash, oldHash);
        
        return senderUpdated;
    }
    public override async Task<int> DeleteUserHashesAsync(string userHash)
    {
        var deleted = await db.Database.ExecuteSqlRawAsync(@"
        DELETE FROM ""GroupMessages""
        WHERE ""SenderHash"" = {0}",userHash);

        return deleted;
    }
    public async Task<List<GroupMessage>> GetMessageStoryAsync(Guid groupId, int take) =>
        await db.GroupMessages.Where(u=> u.GroupId == groupId)
            .OrderByDescending(u => u.SendTime)
            .Take(take)
            .ToListAsync();
    public override async Task<GroupMessage> ReplyMessageAsync(Guid replyId, GroupMessage message)
    {
        message.Reply(replyId);
        await db.SaveChangesAsync();
        return message;
    }
}