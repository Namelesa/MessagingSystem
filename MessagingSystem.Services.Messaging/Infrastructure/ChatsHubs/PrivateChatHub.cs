using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MessagingSystem.Services.Messaging.Application.Chats;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Application.Messages.Dto;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs
{
    [Authorize]
    public class PrivateChatHub(
        ILogger<PrivateChatHub> logger, 
        IMessageOrchestrator messageOrchestrator,
        IChatOrchestrator chatOrchestrator)
        : Hub
    {
        
        private string? CurrentUserNickname =>
            Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;

        private string GetNicknameOrThrow()
        {
            if (CurrentUserNickname is { } nickname)
                return nickname;

            throw new HubException("Unauthorized");
        }
        
        public override async Task OnConnectedAsync()
        {
            var nickname = CurrentUserNickname;
            
            if (!string.IsNullOrEmpty(nickname))
            {
                logger.LogInformation($"User {nickname} connected to chat hub");
                await Groups.AddToGroupAsync(Context.ConnectionId, nickname);
            }
            else
            {
                logger.LogWarning("User connected but no nickname found in claims");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var nickname = GetNicknameOrThrow();
            
            if (!string.IsNullOrEmpty(nickname))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, nickname);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<object> SendPrivateMessage(string recipientNickname, string message)
        {
            var senderNickname = GetNicknameOrThrow();

            if (string.IsNullOrEmpty(senderNickname))
            {
                logger.LogWarning("Attempt to send message without valid sender nickname");
                throw new HubException("Sender identification failed");
            }

            logger.LogInformation($"Sending message from {senderNickname} to {recipientNickname}");

            var dto = new MessagesDto(senderNickname, recipientNickname, message);

            var result = await messageOrchestrator.SendMessageAsync(dto);
            
            if (!result.Success)
            {
                logger.LogWarning($"Failed to save message: {result.Message}");
                throw new HubException("Message could not be saved");
            }

            if (result.Data == null)
                return new { };

            var resultData = new
            {
                messageId = result.Data.MessageId,
                sender = senderNickname,
                content = message
            };
            
            await Clients.Group(senderNickname).SendAsync("ReceivePrivateMessage", resultData);
            await Clients.Group(recipientNickname).SendAsync("ReceivePrivateMessage", resultData);
            
            return resultData;
        }

        public async Task EditMessageAsync(Guid messageId, string content)
        {
            var senderNickname = GetNicknameOrThrow();

            if (string.IsNullOrEmpty(senderNickname))
                throw new HubException("Unauthorized");

            logger.LogInformation($"Attempting to edit message {messageId} by {senderNickname} with new content: {content}");

            var dto = new EditMessageDto(content);
            var result = await messageOrchestrator.EditMessageAsync(messageId, dto);

            if (!result.Success)
            {
                logger.LogWarning($"Failed to edit message: {result.Message}");
                throw new HubException(result.Message);
            }

            logger.LogInformation($"Message {messageId} edited successfully.");

            await Clients.Group(senderNickname).SendAsync("MessageEdited", new
            {
                messageId,
                newContent = content
            });

            var message = await messageOrchestrator.FindMessageByIdAsync(messageId);
            if (message.Data != null && message.Data != senderNickname)
            {
                await Clients.Group(message.Data).SendAsync("MessageEdited", new
                {
                    messageId,
                    newContent = content
                });
            }
        }

        public async Task<List<object>> LoadPrivateChatHistory(string withUser, int take = 50)
        {
            var currentUser = GetNicknameOrThrow();
            
            if (string.IsNullOrEmpty(currentUser))
                throw new HubException("Unauthorized");

            var messages = await messageOrchestrator.LoadChatHistory(currentUser, withUser, take);

            return messages
                .OrderBy(m => m.Date)
                .Select(m => new {
                    messageId = m.Id,
                    sender = m.Sender,
                    content = m.Content,
                    date = m.Date,
                    isEdited = m.IsEdited
                })
                .ToList<object>();
        }
        
        public async Task<List<string>> GetChatsAsync()
        {
            var nickname = GetNicknameOrThrow();
            
            return await chatOrchestrator.GetChatsAsync(nickname);
        }
    }
}
