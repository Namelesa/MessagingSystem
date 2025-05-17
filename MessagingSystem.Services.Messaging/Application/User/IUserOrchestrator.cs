namespace MessagingSystem.Services.Messaging.Application.User;

public interface IUserOrchestrator
{
    Task<OperationResult<string>> CheckUserAsync(string nickName);
}