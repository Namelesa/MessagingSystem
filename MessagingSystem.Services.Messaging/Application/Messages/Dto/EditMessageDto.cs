namespace MessagingSystem.Services.Messaging.Application.Messages.Dto;

public class EditMessageDto(string content)
{
    public string Content { get; set; } = content;
}