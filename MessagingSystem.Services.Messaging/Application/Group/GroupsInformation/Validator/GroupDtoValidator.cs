using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;

public class GroupDtoValidator : AbstractValidator<GroupDto>
{
    public GroupDtoValidator()
    {
        RuleFor(u => u.GroupName)
            .NotEmpty().WithMessage("Group name cannot be empty.")
            .MinimumLength(1).MaximumLength(350).WithMessage("Group name length must between 1 and 350");
        RuleFor(u => u.Description)
            .MinimumLength(1).MaximumLength(650).WithMessage("Description must be between 1 and 650");
        RuleFor(u => u.Admin)
            .NotEmpty().WithMessage("Admin name cannot be empty.")
            .MinimumLength(4).MaximumLength(80).WithMessage("Admin name length must between 4 and 80");
        RuleFor(g => g.Users)
            .NotNull()
            .Must(users => users.Count is >= 3 and <= 40)
            .WithMessage("Group must have between 3 and 40 users.")
            .Must(users =>
            {
                var normalized = users
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u.Trim().ToLowerInvariant());
                var enumerable = normalized.ToList();
                return enumerable.Distinct().Count() == enumerable.Count;
            })
            .WithMessage("User list contains duplicate or empty entries.");
    }
}