namespace MessagingSystem.Services.Messaging.Application.MessageDto;

public class CreatedMessageResult(Guid messageId, DateTime sentTime)
{
    public Guid MessageId { get; init; } = messageId;
    public DateTime SentTime { get; init; } = sentTime;
}