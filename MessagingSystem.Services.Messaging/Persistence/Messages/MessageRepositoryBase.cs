using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace MessagingSystem.Services.Messaging.Persistence.Messages;

public abstract class MessageRepositoryBase<TMessage>(DbContext db) : IMessageRepository<TMessage>
    where TMessage : class
{
    public virtual async Task<List<TMessage>?> FindMessagesAsync(MessageFilter filter)
    {
        var query = BuildQuery(filter);

        return await query.ToListAsync();
    }

    protected virtual IQueryable<TMessage> BuildQuery(MessageFilter filter)
    {
        return db.Set<TMessage>().AsQueryable();
    }

    public abstract Task<TMessage?> FindMessageByIdAsync(Guid id);
    public abstract Task<TMessage> ReplyMessageAsync(Guid replyId, TMessage message);
    public abstract Task<TMessage?> CreateMessageAsync(TMessage message);
    public abstract Task<TMessage> EditMessageAsync(TMessage message);
    public abstract Task<TMessage> DeleteMessageAsync(TMessage message);
    public abstract Task<TMessage> SoftDeleteMessageAsync(TMessage message);
    public abstract Task<int> UpdateUserHashesAsync(string oldHash, string newNick, string newHash);
    public abstract Task<int> DeleteUserHashesAsync(string userHash);
}