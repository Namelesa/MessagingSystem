using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;

public class GroupMemberRepository(GroupAppDbContext db) : IGroupMembersRepository
{
    public async Task<List<GroupMembers>?> FindUserByHashAsync(string userHash)
    {
        if (string.IsNullOrWhiteSpace(userHash))
            return null;

        return await db.GroupMembers
            .Where(m => m.UserNickNameHash == userHash)
            .ToListAsync();
    }

    public async Task<string> EditUserInfoAsync(GroupMembers groupMembers)
    {
        try
        {
            db.GroupMembers.Update(groupMembers);
            await db.SaveChangesAsync();
            return "Edit is ok";
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}