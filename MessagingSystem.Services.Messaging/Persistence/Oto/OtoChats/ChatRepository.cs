using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Oto.OtoChats;

public class ChatRepository(OtoAppDbContext db) : IChatRepository
{
    public async Task<List<Chat>?> GetChatsAsync(string currentUserName)
    {
        return await db.UsersMessages
            .Where(m => !m.IsDeleted &&
                        (m.SenderHash == currentUserName ||
                         m.RecipientHash == currentUserName))
            .Select(m => m.SenderHash == currentUserName ? m.Recipient : m.Sender)
            .Distinct()
            .Join(db.Images,
                nickname => nickname,
                ui => ui.NickNameHash,
                (nickname, ui) => new Chat
                {
                    NickName = nickname,
                    Image = ui.Image
                })
            .ToListAsync();
    }
}