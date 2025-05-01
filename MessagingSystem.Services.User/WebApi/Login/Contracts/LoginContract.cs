using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.Login.Contracts;

public class LoginContract(string login, string password)
{
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{5,20}$", 
        ErrorMessage = "Login must be 5 to 20 characters long and include at least one special character (!, _, @).")]
    public string Login { get; set; } = login;
    
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password, ErrorMessage = "Invalid password format")]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$",
        ErrorMessage =
            "Password must be 5 to 15 characters long and include at least one letter, " +
            "one number, and one special character (!, _, @).")]
    public string Password { get; set; } = password;
}