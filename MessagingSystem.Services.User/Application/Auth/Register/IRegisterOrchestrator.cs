using MessagingSystem.Services.User.Application.Auth.Register.Dto;

namespace MessagingSystem.Services.User.Application.Auth.Register;

public interface IRegisterOrchestrator
{
    Task<OperationResult<string>> RegisterUserAsync(RegisterDto registerDto);
    Task<OperationResult<string>> ConfirmEmailAsync(string hashNickName);
}