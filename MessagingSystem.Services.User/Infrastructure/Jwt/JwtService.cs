namespace MessagingSystem.Services.User.Infrastructure.Jwt;

public class JwtService : IJwtService
{
    public Task<string?> AuthenticateAsync<T>(T t, string password)
    {
        throw new NotImplementedException();
    }
}