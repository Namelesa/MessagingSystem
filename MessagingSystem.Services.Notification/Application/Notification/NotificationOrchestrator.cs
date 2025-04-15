using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Infrastructure.Encrypt;

namespace MessagingSystem.Services.Notification.Application.Notification;

public class NotificationOrchestrator(
    INotification notification,
    IEncryptInfo encryptInfo,
    IValidator<UserDto> validator)
{
    private async Task<OperationResult<string>> SendEmailAsync(UserDto userDto, Func<UserDto, Task<bool>> sendEmail)
    {
        var validationResult = await ValidateAndEncryptAsync(userDto);
        if (!validationResult.IsValid)
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        var result = await sendEmail(userDto);
        
        return result
            ? OperationResult<string>.Ok("User notified")
            : OperationResult<string>.Fail("User was not notified");
    }

    public Task<OperationResult<string>> SendConfirmEmailAsync(UserDto userDto)
        => SendEmailAsync(userDto, async u =>
        {
            var link = GenerateLink(u.NickName);
            return await notification.SendConfirmEmailAsync(u, link);
        });

    public Task<OperationResult<string>> SendEditUserInfoEmailAsync(UserDto userDto)
        => SendEmailAsync(userDto, notification.SendEditUserInfoEmailAsync);

    public Task<OperationResult<string>> SendDeleteUserInfoEmailAsync(UserDto userDto)
        => SendEmailAsync(userDto, notification.SendDeleteUserEmailAsync);

    private async Task<ValidationResult> ValidateAndEncryptAsync(UserDto userDto)
    {
        if (encryptInfo is EncryptInfo concreteEncryptor)
        {
            concreteEncryptor.DecryptObjectStrings(userDto);
        }
        var validationResult = await validator.ValidateAsync(userDto);
        return validationResult;
    }

    private static string GenerateLink(string nickName)
    {
        const string baseUrl = "https://localhost:7210/api/auth/";
        return $"{baseUrl}confirm-email?id={nickName}";
    }
}
