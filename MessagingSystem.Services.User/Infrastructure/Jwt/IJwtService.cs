namespace MessagingSystem.Services.User.Infrastructure.Jwt;

public interface IJwtService
{
    Task<string?> AuthenticateAsync<T>(T t, string password);
}