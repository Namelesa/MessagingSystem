using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;

public class ChatRepository(OtoAppDbContext db) : IChatRepository
{
    public async Task<List<string>?> GetChatsAsync(string currentUserName)
    {
        return await db.UsersMessages
            .Where(m => !m.IsDeleted &&
                        (m.SenderHash == currentUserName || m.RecipientHash == currentUserName))
            .Select(m => m.SenderHash == currentUserName ? m.Recipient : m.Sender)
            .Distinct()
            .ToListAsync();
    }
}