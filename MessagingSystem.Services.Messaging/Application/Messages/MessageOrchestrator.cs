using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Messages.Dto;
using MessagingSystem.Services.Messaging.Core.Messages;

namespace MessagingSystem.Services.Messaging.Application.Messages;

public class MessageOrchestrator(
    IMessageRepository messageRepository, 
    IMapper mapper
    ) : IMessageOrchestrator
{
    public async Task<OperationResult<CreatedMessageResult>> SendMessageAsync(MessagesDto messagesDto)
    {
        var message = mapper.Map<Message>(messagesDto);
        var result = await messageRepository.CreateMessageAsync(message);
        
        return result != null
            ? OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(result.Recipient, result.Id))
            : OperationResult<CreatedMessageResult>.Fail("Failed to send message");
    }
    public async Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<string>.Fail("Message was not founded");
        
        message.EditInfo(messagesDto.Content);
        var result = await messageRepository.EditMessageAsync(message);
        
        return OperationResult<string>.Ok(result.Sender);
    }
    public async Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        return message == null 
            ? OperationResult<string>.Fail("Message was not founded") 
            : OperationResult<string>.Ok(message.Sender);
    }

    public async Task<List<Message>> LoadChatHistory(string sender, string recipient, int take)
    {
        var messageList = await messageRepository.GetMessageStoryAsync(sender, recipient, take);

        return messageList;
    } 
}