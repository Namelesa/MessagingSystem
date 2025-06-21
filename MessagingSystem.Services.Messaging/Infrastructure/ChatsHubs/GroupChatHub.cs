using System.Security.Claims;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;

[Authorize]
public class GroupChatHub(
    ILogger<GroupChatHub> logger,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IGroupMessagesOrchestrator groupMessagesOrchestrator,
    IMapper mapper,
    IHasher hasher
) : Hub
{
    private string CurrentUserNickname =>
        Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value
        ?? throw new HubException("Unauthorized");
    public override async Task OnConnectedAsync()
    {
        var nickname = CurrentUserNickname;
        logger.LogInformation("User {Nickname} connected to group chat hub", nickname);
        await Groups.AddToGroupAsync(Context.ConnectionId, nickname);
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var nickname = CurrentUserNickname;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, nickname);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task<List<GroupDto>> GetAllGroupForUserAsync()
    {
        var nickname = CurrentUserNickname;
        var groups = await groupInfoOrchestrator.GetGroupsForUserAsync(nickname);
        return groups is { Success: true, Data: not null } 
            ? groups.Data
            : [];
    }
    public async Task<GroupDto> CreateGroupAsync(CreateGroup groupInfo)
    {
        groupInfo.Admin = CurrentUserNickname;
        
        var dto = mapper.Map<GroupDto>(groupInfo);
        var result = await groupInfoOrchestrator.CreateGroupAsync(dto);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Failed to create group");
    
        await Groups.AddToGroupAsync(Context.ConnectionId, result.Data.GroupName);

        return result.Data;
    }
    public async Task<GroupDto> EditGroupAsync(Guid groupId, EditGroup groupInfo)
    {
        var dto = mapper.Map<EditGroupDto>(groupInfo);

        var result = await groupInfoOrchestrator.EditGroupInfoAsync(groupId, dto);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Failed to edit group");

        return result.Data;
    }
    public async Task<string> DeleteGroupAsync(Guid groupId)
    {
        var result = await groupInfoOrchestrator.DeleteGroupInfoAsync(groupId, CurrentUserNickname);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Failed to delete group");

        return result.Data;
    }
    public async Task<GroupDto> AddMembersToGroupAsync(Guid groupId, AddMembers members)
    {
        var dto = mapper.Map<GroupMembersDto>(members);
        var adminHash = hasher.Hash(CurrentUserNickname);

        var result = await groupInfoOrchestrator.AddMembersToGroupAsync(groupId, dto, adminHash);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Failed to add members");
        
        await NotifyUsersInGroupAsync(groupId.ToString(), "GroupMembersAdded", result.Data);

        return result.Data;
    }
    public async Task<GroupDto> RemoveMembersFromGroupAsync(Guid groupId, AddMembers members)
    {
        var dto = mapper.Map<GroupMembersDto>(members);
        var adminHash = hasher.Hash(CurrentUserNickname);

        var result = await groupInfoOrchestrator.DeleteMembersFromGroupAsync(groupId, dto, adminHash);

        if (!result.Success || result.Data == null)
            throw new HubException(result.Message ?? "Failed to remove members");

        await NotifyUsersInGroupAsync(groupId.ToString(), "GroupMembersRemoved", result.Data);

        return result.Data;
    }
    public Task JoinGroupAsync(Guid groupId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }
    public Task LeaveGroupAsync(Guid groupId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId.ToString());
    }
    public async Task<List<GroupMessage>> LoadChatHistoryAsync(Guid groupId, int take)
    {
        var messages = await groupMessagesOrchestrator.LoadChatHistory(groupId, take);
        return messages;
    }
    public async Task<CreatedMessageResult> SendMessageAsync(string content, Guid groupId)
    {
        var nickname = CurrentUserNickname;
        
        var message = mapper.Map<GroupMessageDto>(new CreateGroupMessage(nickname, content, groupId));
        
        var result = await groupMessagesOrchestrator.SendMessageAsync(message);

        if (result?.Data == null)
            throw new HubException("Failed to send message");
        await NotifyUsersInGroupAsync(message.GroupId.ToString(), "ReceiveMessage", message);
        return result.Data;
    }
    public async Task EditMessageAsync(Guid messageId, string content)
    {
        
        var result = await groupMessagesOrchestrator.EditMessageAsync(messageId, new EditMessageDto(content));

        if (!result.Success)
            throw new HubException(result.Message ?? "Failed to edit message");

        var editInfo = new
        {
            messageId,
            newContent = content,
            editedAt = DateTime.UtcNow
        };

        await NotifyUsersInGroupAsync(result.Data, "MessageEdited", editInfo);
    }
    public async Task SofDeleteMessageAsync(Guid messageId)
    {
        var result = await groupMessagesOrchestrator.SoftDeleteMessageAsync(messageId);

        if (!result.Success)
            throw new HubException(result.Message ?? "Failed to soft delete message");

        await NotifyUsersInGroupAsync(result.Data, "MessageSoftDeleted", messageId);
    }
    public async Task DeleteMessageAsync(Guid messageId)
    {
        var result = await groupMessagesOrchestrator.DeleteMessageAsync(messageId);

        if (!result.Success)
            throw new HubException(result.Message ?? "Failed to delete message");

        await NotifyUsersInGroupAsync(result.Data, "MessageDeleted", messageId);
    }
    public async Task<object> ReplyForMessageAsync(Guid messageId, string message, Guid groupId)
    {
        var sender = CurrentUserNickname;
        
        var sendResult = await groupMessagesOrchestrator.SendMessageAsync(new GroupMessageDto(sender, message, groupId));
        if (!sendResult.Success || sendResult.Data == null)
            throw new HubException(sendResult.Message ?? "Failed to send message");

        var replyResult = await groupMessagesOrchestrator.ReplyForMessageAsync(sendResult.Data.MessageId, messageId);
        if (!replyResult.Success || replyResult.Data == null)
            throw new HubException(replyResult.Message ?? "Failed to attach reply");

        var resultData = new
        {
            messageId = replyResult.Data.Id,
            sender,
            content = replyResult.Data.Content,
            sentAt = replyResult.Data.SendTime,
            replyTo = messageId
        };

        await NotifyUsersInGroupAsync(groupId.ToString(), "MessageReplied", resultData);
        return resultData;
    }
    public async Task<List<object>> FindMessageByFilterAsync(string? recipient, DateTime? time, string? sender)
    {
        var filter = new MessageFilter()
        {
            Sender = sender,
            Recipient = recipient,
            Date = time
        };
        
        var messages = await groupMessagesOrchestrator.FindMessagesAsync(filter);
        
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
    private Task NotifyUsersInGroupAsync(string groupId, string method, object data)
    {
        return Clients.Group(groupId).SendAsync(method, data);
    }
}