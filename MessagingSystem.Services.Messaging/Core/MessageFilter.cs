namespace MessagingSystem.Services.Messaging.Core;

public class MessageFilter
{
    public string? Sender { get; init; }
    public string? Recipient { get; init; }
    public DateTime? Date { get; init; }
}
