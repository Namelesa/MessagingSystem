namespace MessagingSystem.Services.Messaging.Application.Messages.Dto;

public class CreatedMessageResult(string recipient, Guid messageId)
{
    public string Recipient { get; set; } = recipient;
    public Guid MessageId { get; set; } = messageId;
}