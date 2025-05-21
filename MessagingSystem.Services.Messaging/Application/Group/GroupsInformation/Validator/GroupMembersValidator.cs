using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;

public class GroupMembersValidator : AbstractValidator<GroupMembersDto>
{
    public GroupMembersValidator()
    {
        RuleFor(g => g.Users)
            .NotNull()
            .Must(users => users.Count is >= 1 and <= 37)
            .WithMessage("Group must have between 1 and 40 users. 37");
    }
}