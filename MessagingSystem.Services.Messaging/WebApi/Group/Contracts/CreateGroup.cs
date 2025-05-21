using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Contracts;

public class CreateGroup
{
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(350, MinimumLength = 1, ErrorMessage = "Group name length must be between 1 and 350 characters.")]
    public string GroupName { get; set; }
    public string? Image { get; set; }

    [StringLength(650, MinimumLength = 1, ErrorMessage = "Group description length must be between 1 and 650 characters.")]
    public string Description { get; set; }
    
    [Required(ErrorMessage = "Group admin is required")]
    [StringLength(80, MinimumLength = 4, ErrorMessage = "Admin name length must be between 1 and 80 characters.")]
    public string Admin { get; set; }
    
    public List<string> Users { get; set; } = [];

    public CreateGroup(string groupName, string image, string description, string admin)
    {
        GroupName = groupName;
        Image = image;
        Description = description;
        Admin = admin;
        
        Users.Add(admin);
    }
}