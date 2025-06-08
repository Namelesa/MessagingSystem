namespace MessagingSystem.Services.Messaging.Application.MessageDto;

public class EditMessageDto(string content)
{
    public string Content { get; init; } = content;
}