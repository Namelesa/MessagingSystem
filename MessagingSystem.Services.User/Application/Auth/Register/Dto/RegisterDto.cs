namespace MessagingSystem.Services.User.Application.Auth.Register.Dto;

public class RegisterDto(
    string email,
    string login,
    string firstName,
    string lastName,
    string nickName,
    string password,
    string image)
{
    public string FirstName { get; init; } = firstName;
    public string LastName { get; init; } = lastName;
    public string Email { get; init; } = email;
    public string Login { get; init; } = login;
    public string NickName { get; init; } = nickName;
    public string Password { get; set; } = password;
    public string? Image { get; set; } = image;
}