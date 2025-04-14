using FluentValidation;

namespace MessagingSystem.Services.User.Application.Auth.Login;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(u => u.Login)
            .NotEmpty().WithMessage("Login cannot be empty.")
            .MinimumLength(5).WithMessage("Login must not be less than 5 characters.")
            .MaximumLength(20).WithMessage("Last name must be at most 25 characters long.")
            .Matches("^(?=.*[!_@])[a-zA-Z0-9!_@]+$")
            .WithMessage("Login name must be 3 to 15 characters long and " +
                         "include at least one special character (!, _, @).");
        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Nick name cannot be empty.")
            .Matches(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$")
            .WithMessage("Password must be 5 to 15 characters long and " +
                         "include at least one letter, one number, and one special character (!, _, @).");
    }
}