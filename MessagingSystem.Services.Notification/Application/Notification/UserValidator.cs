using FluentValidation;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Notification;

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(u => u.UserName)
            .NotEmpty().WithMessage("User name cannot be empty.")
            .MinimumLength(6).WithMessage("User name must not be less than 6 characters.")
            .MaximumLength(100).WithMessage("User name must be at most 100 characters long.");
    }
}