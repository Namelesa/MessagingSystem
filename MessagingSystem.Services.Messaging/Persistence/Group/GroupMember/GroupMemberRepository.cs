using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;

public class GroupMemberRepository(IDbContextFactory<GroupAppDbContext> dbFactory) : IGroupMembersRepository
{
    private async Task<TResult> WithContextAsync<TResult>(Func<GroupAppDbContext, Task<TResult>> action)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await action(context);
    }
    public Task<List<GroupMembers>> FindUserByHashAsync(string userHash)
    {
        if (string.IsNullOrWhiteSpace(userHash))
            return Task.FromResult<List<GroupMembers>>([]);

        return WithContextAsync(context =>
            context.GroupMembers
                .Where(m => m.UserNickNameHash == userHash)
                .ToListAsync());
    }
    public Task<string> EditUserInfoAsync(GroupMembers groupMember)
    {
        return WithContextAsync(async context =>
        {
            try
            {
                context.GroupMembers.Update(groupMember);
                await context.SaveChangesAsync();
                return "Edit is ok";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        });
    }
    public Task<int> DeleteUserInfoAsync(string userHashName)
    {
        return WithContextAsync(async context =>
        {
            var affectedRows = 0;

            affectedRows += await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""GroupMembers""
                WHERE ""UserNickNameHash"" = {0}", userHashName);

            affectedRows += await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""GroupInfos""
                WHERE ""AdminHash"" = {0}", userHashName);

            return affectedRows;
        });
    }
    
    public Task<int> DeleteUsersByHashesAsync(IEnumerable<string> userHashes)
    {
        return WithContextAsync(async context =>
        {
            var hashes = userHashes.ToArray();

            var sql = @"
            DELETE FROM ""GroupMembers""
            WHERE ""UserNickNameHash"" = ANY(@p0)";

            var param = new NpgsqlParameter("p0", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = hashes
            };

            return await context.Database.ExecuteSqlRawAsync(sql, param);
        });
    }
}