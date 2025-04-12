using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.User.Contracts;

public class EditUserContract(string firstName, string lastName, string login, string email)
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "First name length must be between 3 and 25 characters.")]
    public string FirstName { get; set; } = firstName;
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "Last name length must be between 3 and 25 characters.")]
    public string LastName { get; set; } = lastName;
    
    [Required(ErrorMessage = "Login is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{5,20}$", 
        ErrorMessage = "Login must be 5 to 20 characters long and include at least one special character (!, _, @).")]
    public string Login { get; set; } = login;
    
    [Required]
    [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email")]
    public string Email { get; set; } = email;
}