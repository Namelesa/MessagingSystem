using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;

public interface IMessageOrchestrator : IMessageOrchestratorBase<Message, MessagesDto>
{
    Task<List<Message>> LoadChatHistory(string sender, string recipient, int skip, int take);
}