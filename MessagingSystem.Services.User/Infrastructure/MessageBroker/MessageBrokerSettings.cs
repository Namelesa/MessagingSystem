namespace MessagingSystem.Services.User.Infrastructure.MessageBroker;

public class MessageBrokerSettings
{
    public string Host { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}