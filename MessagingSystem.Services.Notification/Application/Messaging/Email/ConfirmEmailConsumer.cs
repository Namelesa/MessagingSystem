using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Messaging.Email;

public class ConfirmEmailConsumer(NotificationOrchestrator notificationOrchestrator) : IConsumer<ConfirmUserEmail>
{
    public async Task Consume(ConsumeContext<ConfirmUserEmail> context)
    {
        var info = context.Message;
        var userDto = new UserDto(info.UserName, info.Email);
        
        try
        {
            var result = await notificationOrchestrator.SendConfirmEmailAsync(userDto, info.NickName);
            Console.WriteLine(result.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}