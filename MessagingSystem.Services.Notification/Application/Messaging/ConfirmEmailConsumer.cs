using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Messaging;

public class ConfirmEmailConsumer(NotificationOrchestrator notificationOrchestrator) : IConsumer<ConfirmUserEmail>
{
    public async Task Consume(ConsumeContext<ConfirmUserEmail> context)
    {
        var info = context.Message;
        var userDto = new UserDto(info.UserName, info.Email, info.NickName);

        try
        {
            var result = await notificationOrchestrator.SendConfirmEmailAsync(userDto);
            Console.WriteLine(result.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}