using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;

public class GroupInfoRepository(GroupAppDbContext db) : IGroupInfoRepository
{
    public async Task<GroupInfo?> FindGroupByIdAsync(Guid id) =>
        await db.GroupInfos.FindAsync(id);

    public async Task<GroupInfo?> FindGroupByNameAsync(string groupName) =>
        await db.GroupInfos.FirstOrDefaultAsync(u => u.GroupName == groupName);

    public async Task<GroupInfo> EditGroupInfoAsync(GroupInfo groupInfo)
    {
        db.Update(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }

    public async Task<GroupInfo> DeleteGroupAsync(GroupInfo groupInfo)
    {
        db.Remove(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }

    public async Task<GroupInfo> CreateGroupAsync(GroupInfo groupInfo)
    {
        await db.AddAsync(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }
}