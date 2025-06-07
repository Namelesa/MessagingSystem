using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.User.Contracts;

public class EditUserContract(string firstName, string lastName, string login, string email, string nickName, string? image = null)
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "First name length must be between 3 and 25 characters.")]
    public string FirstName { get; } = firstName;
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "Last name length must be between 3 and 25 characters.")]
    public string LastName { get; } = lastName;
    
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{5,20}$", 
        ErrorMessage = "Login must be 5 to 20 characters long and include at least one special character (!, _, @).")]
    public string Login { get; } = login;
    
    [Required]
    [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email")]
    public string Email { get; } = email;

    [Required(ErrorMessage = "NickName is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{3,15}$", 
        ErrorMessage = "Nick name must be 3 to 15 characters long and " +
                       "include at least one special character (!, _, @).")]
    public string NickName { get; } = nickName;

    public string? Image { get; set; } = image;
}