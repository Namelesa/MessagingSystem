using MessagingSystem.Services.User.Application.Auth.Login.Dto;

namespace MessagingSystem.Services.User.Infrastructure.Jwt;

public interface IJwtService
{
    Task<bool> AuthenticateAndSetCookieAsync(LoginDto? user, string passwordRequest);
}