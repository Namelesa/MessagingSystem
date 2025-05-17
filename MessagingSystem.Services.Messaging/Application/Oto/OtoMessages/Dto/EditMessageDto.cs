namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;

public class EditMessageDto(string content)
{
    public string Content { get; init; } = content;
}