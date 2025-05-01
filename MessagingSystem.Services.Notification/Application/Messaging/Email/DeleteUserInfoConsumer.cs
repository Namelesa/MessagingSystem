using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;

namespace MessagingSystem.Services.Notification.Application.Messaging.Email;

public class DeleteUserInfoConsumer(INotificationOrchestrator notificationOrchestrator) : IConsumer<DeleteUserEmail>
{
    public async Task Consume(ConsumeContext<DeleteUserEmail> context)
    {
        var user = context.Message;

        var userDto = new UserDto(user.UserName, user.Email);
        
        try
        {
            await notificationOrchestrator.SendDeleteUserInfoEmailAsync(userDto);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}