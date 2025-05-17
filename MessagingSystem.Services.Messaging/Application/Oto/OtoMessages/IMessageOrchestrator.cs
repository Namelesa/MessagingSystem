using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;

public interface IMessageOrchestrator
{
    Task<OperationResult<CreatedMessageResult>> SendMessageAsync(MessagesDto messagesDto);
    Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto);
    Task<OperationResult<string>> SoftDeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> DeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId);
    Task<List<Message>> LoadChatHistory(string sender, string recipient, int take);
    Task<OperationResult<Message>> ReplyForMessageAsync(Guid messageId, Guid replyId);
    Task<List<Message>?> FindMessagesAsync(MessageFilter messageFilter);
}