using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Validators;

public class MessageCreateValidator : AbstractValidator<MessagesDto>
{
    public MessageCreateValidator()
    {
        RuleFor(u => u.Sender)
            .NotEmpty().WithMessage("Sender name cannot be empty.")
            .Matches(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$")
            .WithMessage("Sender name must be 5 to 15 characters long and " +
                         "include at least one letter, one number, and one special character (!, _, @).");
        RuleFor(u => u.Recipient)
            .NotEmpty().WithMessage("Sender name cannot be empty.")
            .Matches(@"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[!_@])[a-zA-Z\d!_@]{5,15}$")
            .WithMessage("Sender name must be 5 to 15 characters long and " +
                         "include at least one letter, one number, and one special character (!, _, @).");
        RuleFor(u => u.Content)
            .NotEmpty().WithMessage("Content cannot be empty.")
            .MinimumLength(1).WithMessage("Minimal length of content must be 1")
            .MaximumLength(20000).WithMessage("Maximal length of content must be 2000");
    }
}