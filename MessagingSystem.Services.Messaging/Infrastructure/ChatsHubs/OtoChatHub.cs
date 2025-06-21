using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;

[Authorize]
public class OtoChatHub(
    ILogger<OtoChatHub> logger,
    IMessageOrchestrator messageOrchestrator,
    IChatOrchestrator chatOrchestrator)
    : Hub
{
    private string CurrentUserNickname =>
        Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value
        ?? throw new HubException("Unauthorized");

    public override async Task OnConnectedAsync()
    {
        var nickname = CurrentUserNickname;

        logger.LogInformation("User {Nickname} connected to chat hub", nickname);
        await Groups.AddToGroupAsync(Context.ConnectionId, nickname);

        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var nickname = CurrentUserNickname;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, nickname);

        await base.OnDisconnectedAsync(exception);
    }
    public async Task<object> SendMessageAsync(string recipientNickname, string message)
    {
        var sender = CurrentUserNickname;

        logger.LogInformation("Sending message from {Sender} to {Recipient}", sender, recipientNickname);

        var result = await messageOrchestrator.SendMessageAsync(new MessagesDto(sender, recipientNickname, message));
        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Message could not be saved");

        var messageData = new
        {
            messageId = result.Data.MessageId,
            sender,
            content = message,
            sentAt = result.Data.SentTime
        };

        await NotifyUsersAsync(sender, recipientNickname, "ReceivePrivateMessage", messageData);
        return messageData;
    }
    public async Task EditMessageAsync(Guid messageId, string content)
    {
        var sender = CurrentUserNickname;

        logger.LogInformation("Editing message {MessageId} by {Sender} with content: {Content}", messageId, sender, content);

        var editResult = await messageOrchestrator.EditMessageAsync(messageId, new EditMessageDto(content));
        if (!editResult.Success)
            throw new HubException(editResult.Message);

        var editInfo = new
        {
            messageId,
            newContent = content,
            editedAt = DateTime.UtcNow
        };

        var messageOwner = await messageOrchestrator.FindMessageByIdAsync(messageId);
        await NotifyUsersAsync(sender, messageOwner.Data, "MessageEdited", editInfo);
    }
    public async Task DeleteMessageAsync(Guid messageId, string typeOfDeleting)
    {
        var sender = CurrentUserNickname;
        var isSoft = typeOfDeleting.Equals("soft", StringComparison.OrdinalIgnoreCase);

        var deleteResult = isSoft
            ? await messageOrchestrator.SoftDeleteMessageAsync(messageId)
            : await messageOrchestrator.DeleteMessageAsync(messageId);

        logger.LogInformation(deleteResult.Data);

        var messageOwner = await messageOrchestrator.FindMessageByIdAsync(messageId);
        if (messageOwner.Data == null) return;

        var deletedInfo = new
        {
            messageId,
            deletedAt = DateTime.UtcNow,
            type = isSoft ? "soft" : "hard"
        };

        await NotifyUsersAsync(sender, messageOwner.Data, "MessageDeleted", deletedInfo);
    }
    public async Task<List<object>> LoadChatHistoryAsync(string withUser, int take = 50)
    {
        var currentUser = CurrentUserNickname;

        var messages = await messageOrchestrator.LoadChatHistory(currentUser, withUser, take);
        return messages.Select(m => new
        {
            messageId = m.Id,
            sender = m.Sender,
            content = m.Content,
            sentAt = m.SendTime,
            isEdited = m.IsEdited,
            editedAt = m.EditDate,
            replyFor = m.ReplyFor
        }).Cast<object>().ToList();
    }
    public async Task<List<ChatDto>?> GetChatsAsync()
    {
        var nickname = CurrentUserNickname;
        return await chatOrchestrator.GetChatsAsync(nickname);
    }
    public async Task<object> ReplyToMessageAsync(string recipientNickname, string message, Guid replyToMessageId)
    {
        var sender = CurrentUserNickname;

        logger.LogInformation("User {Sender} replying to message {ReplyToId} for {Recipient}", sender, replyToMessageId, recipientNickname);

        var sendResult = await messageOrchestrator.SendMessageAsync(new MessagesDto(sender, recipientNickname, message));
        if (!sendResult.Success || sendResult.Data == null)
            throw new HubException(sendResult.Message ?? "Failed to send message");

        var replyResult = await messageOrchestrator.ReplyForMessageAsync(sendResult.Data.MessageId, replyToMessageId);
        if (!replyResult.Success || replyResult.Data == null)
            throw new HubException(replyResult.Message ?? "Failed to attach reply");

        var resultData = new
        {
            messageId = replyResult.Data.Id,
            sender,
            content = replyResult.Data.Content,
            sentAt = replyResult.Data.SendTime,
            replyTo = replyToMessageId
        };

        await NotifyUsersAsync(sender, recipientNickname, "ReceivePrivateMessage", resultData);
        return resultData;
    }
    public async Task<List<object>> FindMessagesAsync(string? recipient, DateTime? time, string? sender)
    {
        var filter = new MessageFilter
        {
            Sender = sender,
            Date = time
        };

        var messages = await messageOrchestrator.FindMessagesAsync(filter);
        if (messages == null || !messages.Any())
            return [];

        return messages.Select(m => new
        {
            messageId = m.Id,
            sender = m.Sender,
            content = m.Content,
            sentAt = m.SendTime,
            isEdited = m.IsEdited,
            replyFor = m.ReplyFor
        }).Cast<object>().ToList();
    }
    private Task NotifyUsersAsync(string user1, string? user2, string method, object data)
    {
        var tasks = new List<Task> { Clients.Group(user1).SendAsync(method, data) };
        if (!string.IsNullOrEmpty(user2) && user2 != user1)
            tasks.Add(Clients.Group(user2).SendAsync(method, data));

        return Task.WhenAll(tasks);
    }
}