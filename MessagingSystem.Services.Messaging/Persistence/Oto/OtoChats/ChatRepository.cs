using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;

public class ChatRepository(OtoAppDbContext db) : IChatRepository
{
    public async Task<List<Chat>?> GetChatsAsync(string currentUserHash)
    {
        var sql = @"
        SELECT 
    chat_users.user_nickname AS ""NickName"",
    MAX(i.""Image"") AS ""Image""
FROM (
    SELECT 
        CASE 
            WHEN um.""SenderHash"" = @userHash THEN um.""Recipient""
            ELSE um.""Sender""
        END AS user_nickname,
        CASE 
            WHEN um.""SenderHash"" = @userHash THEN um.""RecipientHash""
            ELSE um.""SenderHash""
        END AS user_hash
    FROM public.""UsersMessages"" um
    WHERE um.""IsDeleted"" = false 
        AND (um.""SenderHash"" = @userHash OR um.""RecipientHash"" = @userHash)
        AND um.""SenderHash"" != um.""RecipientHash""
) chat_users
LEFT JOIN public.""Images"" i ON i.""NickNameHash"" = chat_users.user_hash
GROUP BY chat_users.user_nickname;
";

        var param = new NpgsqlParameter("userHash", currentUserHash);
        var chats = await db.Database.SqlQueryRaw<Chat>(sql, param).ToListAsync();
        return chats;
    }
}