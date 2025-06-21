using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;

public class ChatRepository(OtoAppDbContext db) : IChatRepository
{
    public async Task<List<Chat>?> GetChatsAsync(string currentUserHash)
    {
        var sql = @"
        SELECT DISTINCT 
            CASE 
                WHEN um.""SenderHash"" = {0} THEN um.""Recipient"" 
                ELSE um.""Sender"" 
            END as ""NickName"",
            i.""Image""
        FROM public.""UsersMessages"" um
        LEFT JOIN public.""Images"" i ON (
            CASE 
                WHEN um.""SenderHash"" = {0} THEN um.""RecipientHash"" 
                ELSE um.""SenderHash"" 
            END = i.""NickNameHash""
        )
        WHERE um.""IsDeleted"" = false 
            AND (um.""SenderHash"" = {0} OR um.""RecipientHash"" = {0})";

        return await db.Database
            .SqlQueryRaw<Chat>(sql, currentUserHash)
            .ToListAsync();
    }
}