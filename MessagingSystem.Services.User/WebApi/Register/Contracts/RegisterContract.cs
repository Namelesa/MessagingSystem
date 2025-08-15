using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.Register.Contracts;

public class RegisterContract
{
    public RegisterContract() { }
    
    public RegisterContract(
        string firstName, 
        string lastName, 
        string login, 
        string email, 
        string nickName, 
        string password, 
        IFormFile? image = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Login = login;
        Email = email;
        NickName = nickName;
        Password = password;
        Image = image;
    }
    
    [Required(ErrorMessage = "First name is required")]
    [RegularExpression("^[A-Za-zА-Яа-яЁё]{3,25}$", 
        ErrorMessage = "First name must be 3 to 20 characters long and include only letters")]
    public string FirstName { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    [RegularExpression("^[A-Za-zА-Яа-яЁё]{3,25}$", 
        ErrorMessage = "Last name must be 3 to 20 characters long and include only letters")]
    public string LastName { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{5,20}$", 
        ErrorMessage = "Login must be 5 to 20 characters long and include at least one special character (!, _, @).")]
    public string Login { get; init; } = string.Empty;
    
    [Required]
    [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email")]
    public string Email { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "NickName is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{3,15}$", 
        ErrorMessage = "Nick name must be 3 to 15 characters long and " +
                       "include at least one special character (!, _, @).")]
    public string NickName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password, ErrorMessage = "Invalid password format")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,115}$",
        ErrorMessage =
            "Password must be 5 to 115 characters long and include at least one letter, " +
            "one number, and one special character (!, _, @).")]
    public string Password { get; init; } = string.Empty;

    public IFormFile? Image { get; init; }

    public string? AvatarUrl { get; set; }
}