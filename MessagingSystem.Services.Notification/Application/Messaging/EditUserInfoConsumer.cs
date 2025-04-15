using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Messaging;

public class EditUserInfoConsumer(NotificationOrchestrator notificationOrchestrator) : IConsumer<EditUserEmail>
{
    public async Task Consume(ConsumeContext<EditUserEmail> context)
    {
        var info = context.Message;
        var userDto = new UserDto(info.UserName, info.Email);
        
        try
        {
            var result = await notificationOrchestrator.SendEditUserInfoEmailAsync(userDto);
            Console.WriteLine(result.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}