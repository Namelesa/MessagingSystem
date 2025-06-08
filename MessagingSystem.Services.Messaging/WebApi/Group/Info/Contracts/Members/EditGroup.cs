using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;

public class EditGroup(string groupName, string? image, string? description)
{
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(350, MinimumLength = 1, ErrorMessage = "Group name length must be between 1 and 350 characters.")]
    public string GroupName { get; set; } = groupName;
    
    public string? Image { get; set; } = image;
    
    [StringLength(650, MinimumLength = 1, ErrorMessage = "Group description length must be between 1 and 650 characters.")]
    public string? Description { get; set; } = description;
}
