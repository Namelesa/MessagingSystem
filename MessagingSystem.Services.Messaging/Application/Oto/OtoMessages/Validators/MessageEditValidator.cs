using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Validators;

public class MessageEditValidator : AbstractValidator<EditMessageDto>
{
    public MessageEditValidator()
    {
        RuleFor(u => u.Content)
            .NotEmpty().WithMessage("Content cannot be empty.")
            .MinimumLength(1).WithMessage("Minimal length of content must be 1")
            .MaximumLength(20000).WithMessage("Maximal length of content must be 2000");
    }
}