using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;

public class GroupInfoRepository(GroupAppDbContext db) : IGroupInfoRepository
{
    public async Task<GroupInfo?> FindGroupByIdAsync(Guid id) =>
        await db.GroupInfos
            .Include(u => u.Members)
            .FirstOrDefaultAsync(u => u.Id == id);
    public async Task<GroupInfo?> FindGroupByNameHashAsync(string groupName) =>
        await db.GroupInfos
            .Include(u => u.Members)
            .FirstOrDefaultAsync(u => u.GroupNameHash == groupName);
    public async Task<List<GroupInfo>?> FindGroupByAdminHashAsync(string adminHash) =>
        await db.GroupInfos.Where(m => m.AdminHash == adminHash)
            .ToListAsync();
    public async Task<GroupInfo> EditGroupInfoAsync(GroupInfo groupInfo)
    {
        db.GroupInfos.Update(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }
    public async Task<GroupInfo> DeleteGroupAsync(GroupInfo groupInfo)
    {
        db.GroupInfos.Remove(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }
    public async Task<GroupInfo> CreateGroupAsync(GroupInfo groupInfo)
    {
        await db.GroupInfos.AddAsync(groupInfo);
        await db.SaveChangesAsync();
        return groupInfo;
    }
    public async Task<List<GroupInfo>> GetGroupsByUserAsync(string userHash)
    {
        return await db.GroupInfos
            .Include(g => g.Members)
            .Where(g =>
                g.Members.Any(m => m.UserNickNameHash == userHash) || g.AdminHash == userHash)
            .ToListAsync();
    }
}