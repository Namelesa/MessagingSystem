using System.Security.Claims;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;

[Authorize]
public class GroupChatHub(
    ILogger<GroupChatHub> logger,
    IGroupInfoOrchestrator groupInfoOrchestrator,
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
    private Task NotifyUsersInGroupAsync(string groupId, string method, object data)
    {
        return Clients.Group(groupId).SendAsync(method, data);
    }
}