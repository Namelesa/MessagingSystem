namespace MessagingSystem.Services.Messaging.Core;

public class MessageFilter
{
    public string? Sender { get; set; }
    public string? Recipient { get; set; }
    public DateTime? Date { get; set; }
    public string? Content { get; set; }
}
