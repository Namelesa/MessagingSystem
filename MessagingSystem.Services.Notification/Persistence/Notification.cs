using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace MessagingSystem.Services.Notification.Persistence
{
    public class Notification(IEmailSender emailSender, ITemplateReader templateReader) : INotification
    {
        public async Task<bool> SendConfirmEmailAsync(UserDto userDto, string link)
        {
            var htmlBody = await templateReader.ReadTemplateAsync(Wc.ConfirmEmailTemplate);
            if (htmlBody == null) return false;

            htmlBody = htmlBody.Replace("{UserName}", userDto.UserName)
                               .Replace("{link}", link);

            await emailSender.SendEmailAsync(userDto.Email, Wc.ConfirmEmail, htmlBody);
            return true;
        }

        public async Task<bool> SendEditUserInfoEmailAsync(UserDto userDto)
        {
            var htmlBody = await templateReader.ReadTemplateAsync(Wc.EditUserTemplate);
            if (htmlBody == null) return false;

            htmlBody = htmlBody.Replace("{UserName}", userDto.UserName);

            await emailSender.SendEmailAsync(userDto.Email, Wc.EditUser, htmlBody);
            return true;
        }

        public async Task<bool> SendDeleteUserEmailAsync(UserDto userDto)
        {
            var htmlBody = await templateReader.ReadTemplateAsync(Wc.DeleteUserTemplate);
            if (htmlBody == null) return false;

            htmlBody = htmlBody.Replace("{UserName}", userDto.UserName);

            await emailSender.SendEmailAsync(userDto.Email, Wc.DeleteUser, htmlBody);
            return true;
        }
    }
}
