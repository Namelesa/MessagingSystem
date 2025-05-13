namespace MessagingSystem.Services.Messaging.Core.Messages;

public class Message(string sender, string recipient, string content)
{
    public Guid Id { get; init; }
    public string Sender { get; init; } = sender;
    public string Recipient { get; init; } = recipient;
    public string Content { get; private set; } = content;
    public DateTime Date { get; init; } = DateTime.UtcNow;

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
}