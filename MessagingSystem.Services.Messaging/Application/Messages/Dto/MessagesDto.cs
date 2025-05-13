namespace MessagingSystem.Services.Messaging.Application.Messages.Dto;

public class MessagesDto(
    string sender,
    string recipient,
    string content)
{
    public string Sender { get; init; } = sender;
    public string Recipient { get; init; } = recipient;
    public string Content { get; init; } = content;
}