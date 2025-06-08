using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMessages;

public interface IGroupMessagesOrchestrator
{
    Task<OperationResult<CreatedMessageResult>> SendMessageAsync(GroupMessageDto messagesDto);
    Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto);
    Task<OperationResult<string>> SoftDeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> DeleteMessageAsync(Guid messageId);
    Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId);
    Task<List<GroupMessage>> LoadChatHistory(Guid groupId, int take);
    Task<OperationResult<GroupMessage>> ReplyForMessageAsync(Guid messageId, Guid replyId);
    Task<List<GroupMessage>?> FindMessagesAsync(MessageFilter messageFilter);
    Task<OperationResult<string>> UpdateUserInfoInMessageAsync(string newNickName, string oldUserHashName);
    Task<OperationResult<string>> DeleteUserInfoInMessageAsync(string userHash);
}