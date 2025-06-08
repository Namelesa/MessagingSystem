using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;

public class CreateGroupMessage(string sender, string content, Guid groupId)
{
    [Required(ErrorMessage = "Sender is required")]
    [RegularExpression("^(?=.*[!_@])[a-zA-Z0-9!_@]{3,15}$", 
        ErrorMessage = "Sender name must be 3 to 15 characters long and " +
                       "include at least one special character (!, _, @).")]
    public string Sender { get; init; } = sender;
    
    [Required(ErrorMessage = "Content is required")]
    [StringLength(1900, MinimumLength = 1, ErrorMessage = "Content length must be between 1 and 1900 characters.")]
    public string Content { get; init; } = content;
    
    public Guid GroupId { get; init; } = groupId;
}