using MessagingSystem.Services.Messaging.Application.User;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;

public class PrivateChatHub(IUserOrchestrator userChecker) : Hub
{
    private static readonly Dictionary<string, string> _users = new();
    private static readonly HashSet<(string from, string to)> _knownPairs = new();

    private readonly IUserOrchestrator _userChecker = userChecker;

    public Task Register(string username)
    {
        lock (_users)
        {
            _users[username] = Context.ConnectionId;
        }

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_users)
        {
            var item = _users.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(item.Key))
            {
                _users.Remove(item.Key);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string fromUsername, string toUsername, string message)
    {
        if (!_knownPairs.Contains((fromUsername, toUsername)))
        {
            var exists = await _userChecker.CheckUserAsync(toUsername);
            if (exists != null)
            {
                await Clients.Caller.SendAsync("UserNotFound", toUsername);
                return;
            }

            _knownPairs.Add((fromUsername, toUsername));
        }

        if (_users.TryGetValue(toUsername, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", fromUsername, message);
        }
        else
        {
            // Можно сохранить сообщение в очередь/БД, если получатель offline
            await Clients.Caller.SendAsync("UserOffline", toUsername);
        }
    }
}