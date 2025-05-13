namespace MessagingSystem.Services.User.Application.Auth.Login.Dto;

public class LoginDto(string login, string password, string nickName)
{
    public string Login { get; init; } = login;
    public string Password { get; init; } = password;

    public string NickName { get; set; } = nickName;
}