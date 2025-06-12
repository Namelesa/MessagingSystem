using MessagingSystem.Services.Messaging.Core.Messages;

namespace MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;

public class Message(string sender, string recipient, string content) : IMessageEntity
{
    public Guid Id { get; init; }
    public Guid? ReplyFor { get; private set; }
    public string Sender { get; private set; } = sender;
    public string? SenderHash { get; private set; }
    public string Recipient { get; private set; } = recipient;
    public string? RecipientHash { get; private set; }
    public string Content { get; private set; } = content;
    public DateTime SendTime { get; init; } = DateTime.UtcNow;

    public bool IsDeleted { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditDate { get; private set; }

    public void SoftDeleteInfo()
    {
        IsDeleted = true;
    }
    
    public void EditInfo(string content)
    {
        IsEdited = true;
        Content = content;
        EditDate = DateTime.UtcNow;
    }
    
    public void Reply(Guid replyId)
    {
        ReplyFor = replyId;
    }

    public void SetHashes(string senderHash, string recipientHash)
    {
        SenderHash = senderHash;
        RecipientHash = recipientHash;
    }
}