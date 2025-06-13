using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.User.WebApi.User.Contracts;

public class EditUserContract
{
    public EditUserContract() { }
    
    public EditUserContract(
        string firstName, 
        string lastName, 
        string login, 
        string email, 
        string nickName, 
        string? image = null,
        IFormFile? imageFile = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Login = login;
        Email = email;
        NickName = nickName;
        Image = image;
        ImageFile = imageFile;
    }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "First name length must be between 3 and 25 characters.")]
    public string FirstName { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "Last name length must be between 3 and 25 characters.")]
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

    public string? Image { get; set; } = string.Empty;

    public IFormFile? ImageFile { get; init; }
}