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
            .MaximumLength(50).WithMessage("User name must be at most 50 characters long.")
            .Matches(@"^[A-Za-zА-Яа-яЁё]+$").WithMessage("User name can only contain letters.");
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}