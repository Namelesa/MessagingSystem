using MessagingSystem.Services.User.Application.Auth.Login.Dto;

namespace MessagingSystem.Services.User.Application.Auth.Login;

public interface ILoginOrchestrator
{
    Task<OperationResult<string>> LoginUserAsync(LoginDto loginDto);
}