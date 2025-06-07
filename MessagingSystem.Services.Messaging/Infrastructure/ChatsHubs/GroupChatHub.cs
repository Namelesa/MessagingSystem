using System.Security.Claims;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;

[Authorize]
public class GroupChatHub(
    ILogger<GroupChatHub> logger,
    IGroupInfoOrchestrator groupInfoOrchestrator
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
}