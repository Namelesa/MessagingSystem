using FluentValidation;

namespace MessagingSystem.Services.User.Application.User;

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(u => u.FirstName)
            .NotEmpty().WithMessage("First name cannot be empty.")
            .MinimumLength(3).WithMessage("First name must not be less than 3 characters.")
            .MaximumLength(25).WithMessage("First name must be at most 25 characters long.")
            .Matches(@"^[A-Za-zА-Яа-яЁё]+$").WithMessage("First name can only contain letters.");
        RuleFor(u => u.LastName)
            .NotEmpty().WithMessage("Last name cannot be empty.")
            .MinimumLength(3).WithMessage("Last name must not be less than 3 characters.")
            .MaximumLength(25).WithMessage("Last name must be at most 25 characters long.")
            .Matches(@"^[A-Za-zА-Яа-яЁё]+$").WithMessage("Last name can only contain letters.");
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(u => u.Login)
            .NotEmpty().WithMessage("Login cannot be empty.")
            .MinimumLength(5).WithMessage("Login must not be less than 5 characters.")
            .MaximumLength(20).WithMessage("Last name must be at most 25 characters long.")
            .Matches("^(?=.*[!_@])[a-zA-Z0-9!_@]+$")
            .WithMessage("Login name must be 3 to 15 characters long and " +
                         "include at least one special character (!, _, @).");
        RuleFor(u => u.NickName)
            .NotEmpty().WithMessage("Nick name cannot be empty.")
            .MinimumLength(3).WithMessage("Nick name must not be less than 5 characters.")
            .MaximumLength(15).WithMessage("Last name must be at most 25 characters long.")
            .Matches(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$")
            .WithMessage("Nick name must be 5 to 15 characters long and " +
                         "include at least one letter, one number, and one special character (!, _, @).");
    }
}