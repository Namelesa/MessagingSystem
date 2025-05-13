using MessagingSystem.Services.Messaging.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Application.Chats;

public class ChatOrchestrator(AppDbContext db) : IChatOrchestrator
{
    public async Task<List<string>> GetChatsAsync(string currentUserName)
    {
        var chatPartners = await db.UsersMessages
            .Where(m => !m.IsDeleted && (m.Sender == currentUserName || m.Recipient == currentUserName))
            .Select(m => m.Sender == currentUserName ? m.Recipient : m.Sender)
            .Distinct()
            .ToListAsync();

        return chatPartners;
    }
}