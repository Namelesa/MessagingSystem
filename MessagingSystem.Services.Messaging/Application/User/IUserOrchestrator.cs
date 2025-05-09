namespace MessagingSystem.Services.Messaging.Application.User;

public interface IUserOrchestrator
{
    Task<string> CheckUserAsync(string nickName);
}