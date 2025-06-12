namespace MessagingSystem.Services.Messaging.Core.Messages;

public interface IMessageEntity
{
    Guid Id { get; }
    DateTime SendTime { get; }
}