using System.ComponentModel.DataAnnotations;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;

public class CreateGroup
{
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(350, MinimumLength = 1, ErrorMessage = "Group name length must be between 1 and 350 characters.")]
    public string GroupName { get; init; }
    public string? Image { get; set; }

    [StringLength(650, MinimumLength = 1, ErrorMessage = "Group description length must be between 1 and 650 characters.")]
    public string Description { get; init; }
    
    [Required(ErrorMessage = "Group admin is required")]
    [StringLength(80, MinimumLength = 4, ErrorMessage = "Admin name length must be between 1 and 80 characters.")]
    public string Admin { get; set; }
    
    public List<string> Users { get; set; } = [];
    
    public IFormFile? ImageFile { get; set; }

    public CreateGroup() { }
    
    public CreateGroup(string groupName, string image, string description, string admin, IFormFile? imageFile = null)
    {
        GroupName = groupName;
        Image = image;
        Description = description;
        Admin = admin;
        ImageFile = imageFile;
        Users.Add(admin);
    }
}