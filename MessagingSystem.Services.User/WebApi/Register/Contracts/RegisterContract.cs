using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.Register.Contracts;

public class RegisterContract(string firstName, string lastName, string login, string email, string nickName, string password)
{
    [Required(ErrorMessage = "First name is required")]
    [RegularExpression("^[A-Za-zА-Яа-яЁё]{3,25}$", 
        ErrorMessage = "First name must be 3 to 20 characters long and include only letters")]
    public string FirstName { get; set; } = firstName;
    
    [Required(ErrorMessage = "Last name is required")]
    [RegularExpression("^[A-Za-zА-Яа-яЁё]{3,25}$", 
        ErrorMessage = "Last name must be 3 to 20 characters long and include only letters")]
    public string LastName { get; set; } = lastName;
    
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{5,20}$", 
        ErrorMessage = "Login must be 5 to 20 characters long and include at least one special character (!, _, @).")]
    public string Login { get; set; } = login;
    
    [Required]
    [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email")]
    public string Email { get; set; } = email;
    
    [Required(ErrorMessage = "NickName is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{3,15}$", 
        ErrorMessage = "Nick name must be 3 to 15 characters long and " +
                       "include at least one special character (!, _, @).")]
    public string NickName { get; set; } = nickName;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password, ErrorMessage = "Invalid password format")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$",
        ErrorMessage =
            "Password must be 5 to 15 characters long and include at least one letter, " +
            "one number, and one special character (!, _, @).")]
    public string Password { get; set; } = password;
}