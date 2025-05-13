using MessagingSystem.Services.Messaging.Application.Messages.Dto;
using MessagingSystem.Services.Messaging.Core.Messages;

namespace MessagingSystem.Services.Messaging.Application.Messages;

public interface IMessageOrchestrator
{
    Task<OperationResult<CreatedMessageResult>> SendMessageAsync(MessagesDto messagesDto);
    Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto);
    Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId);
    Task<List<Message>> LoadChatHistory(string sender, string recipient, int take);
}