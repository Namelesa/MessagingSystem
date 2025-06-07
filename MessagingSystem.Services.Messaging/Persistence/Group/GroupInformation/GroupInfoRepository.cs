using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;

public class GroupInfoRepository(IDbContextFactory<GroupAppDbContext> dbContextFactory) : IGroupInfoRepository
{
    private async Task<TResult> WithContextAsync<TResult>(Func<GroupAppDbContext, Task<TResult>> action)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await action(context);
    }

    public Task<GroupInfo?> FindGroupByIdAsync(Guid id) =>
        WithContextAsync(context =>
            context.GroupInfos
                .Include(u => u.Members)
                .FirstOrDefaultAsync(u => u.Id == id));

    public Task<GroupInfo?> FindGroupByNameHashAsync(string groupName) =>
        WithContextAsync(context =>
            context.GroupInfos
                .Include(u => u.Members)
                .FirstOrDefaultAsync(u => u.GroupNameHash == groupName));

    public Task<List<GroupInfo>> FindGroupByAdminHashAsync(string adminHash) =>
        WithContextAsync(context =>
            context.GroupInfos
                .Where(m => m.AdminHash == adminHash)
                .ToListAsync());

    public Task<GroupInfo> EditGroupInfoAsync(GroupInfo groupInfo) =>
        WithContextAsync(async context =>
        {
            context.GroupInfos.Update(groupInfo);
            await context.SaveChangesAsync();
            return groupInfo;
        });

    public Task<GroupInfo> DeleteGroupAsync(GroupInfo groupInfo) =>
        WithContextAsync(async context =>
        {
            context.GroupInfos.Remove(groupInfo);
            await context.SaveChangesAsync();
            return groupInfo;
        });

    public Task<GroupInfo> CreateGroupAsync(GroupInfo groupInfo) =>
        WithContextAsync(async context =>
        {
            await context.GroupInfos.AddAsync(groupInfo);
            await context.SaveChangesAsync();
            return groupInfo;
        });

    public Task<List<GroupInfo>> GetGroupsByUserAsync(string userHash) =>
        WithContextAsync(context =>
            context.GroupInfos
                .Include(g => g.Members)
                .Where(g =>
                    g.Members.Any(m => m.UserNickNameHash == userHash) || g.AdminHash == userHash)
                .ToListAsync());
}
