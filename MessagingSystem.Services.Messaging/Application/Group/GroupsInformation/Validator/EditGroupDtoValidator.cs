using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;

public class EditGroupDtoValidator : AbstractValidator<EditGroupDto>
{
    public EditGroupDtoValidator()
    {
        RuleFor(u => u.GroupName)
            .NotEmpty().WithMessage("Group name cannot be empty.")
            .MinimumLength(1).MaximumLength(350).WithMessage("Group name length must between 1 and 350");
        RuleFor(u => u.Description)
            .MinimumLength(1).MaximumLength(650).WithMessage("Description must be between 1 and 650");
    }
}