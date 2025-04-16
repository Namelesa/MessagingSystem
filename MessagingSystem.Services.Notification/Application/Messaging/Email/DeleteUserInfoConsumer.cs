using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Messaging.Email;

public class DeleteUserInfoConsumer(NotificationOrchestrator notificationOrchestrator) : IConsumer<DeleteUserEmail>
{
    public async Task Consume(ConsumeContext<DeleteUserEmail> context)
    {
        var user = context.Message;

        var userDto = new UserDto(user.UserName, user.Email);
        
        try
        {
            var result = await notificationOrchestrator.SendDeleteUserInfoEmailAsync(userDto);
            Console.WriteLine(result.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}