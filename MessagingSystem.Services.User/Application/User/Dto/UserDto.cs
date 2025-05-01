namespace MessagingSystem.Services.User.Application.User.Dto;

public class UserDto(
    string firstName, 
    string lastName, 
    string login, 
    string email, 
    string nickName)
{
    public string FirstName { get; init; } = firstName;
    public string LastName { get; init; } = lastName;
    public string Login { get; init; } = login;
    public string NickName { get; init; } = nickName;
    public string Email { get; init; } = email;
}