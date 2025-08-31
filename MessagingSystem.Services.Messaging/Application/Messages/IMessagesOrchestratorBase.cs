using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;

namespace MessagingSystem.Services.Messaging.Application.Messages;

public interface IMessageOrchestratorBase<TMessage, TCreateDto>  
    where TMessage : class
    where TCreateDto : class
{
    Task<OperationResult<CreatedMessageResult>> SendMessageAsync(TCreateDto messagesDto);
    Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto);
    Task<OperationResult<string>> SoftDeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> DeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId);
    Task<OperationResult<TMessage>> ReplyForMessageAsync(Guid messageId, Guid replyId);
    Task<List<TMessage>?> FindMessagesAsync(MessageFilter messageFilter);
    Task<OperationResult<string>> UpdateUserInfoInMessageAsync(string newNickName, string oldUserHashName);
    Task<OperationResult<string>> DeleteUserInfoInMessageAsync(string userHash);
}