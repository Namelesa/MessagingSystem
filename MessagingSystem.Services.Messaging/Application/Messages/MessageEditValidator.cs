using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Messages.Dto;

namespace MessagingSystem.Services.Messaging.Application.Messages;

public class MessageEditValidator : AbstractValidator<EditMessageDto>
{
    public MessageEditValidator()
    {
        RuleFor(u => u.Content)
            .NotEmpty().WithMessage("Content cannot be empty.")
            .MinimumLength(1).WithMessage("Minimal length of content must be 1")
            .MaximumLength(2000).WithMessage("Maximal length of content must be 2000");
    }
}