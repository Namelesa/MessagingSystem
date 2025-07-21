using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMessages;

public interface IGroupMessagesOrchestrator : IMessageOrchestratorBase<GroupMessage ,GroupMessageDto>
{
    Task<List<GroupMessage>> LoadChatHistory(Guid groupId, int skip, int take);
}