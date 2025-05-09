using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.User.Application.User;

namespace MessagingSystem.Services.User.Application.Messaging.UserChecker;

public class UserCheckerConsumer(
    IDecryptionInfo decryptionInfo,
    IUserOrchestrator userOrchestrator
    ) : IConsumer<ExistingUserRequest>
{
    public async Task Consume(ConsumeContext<ExistingUserRequest> context)
    {
        var decryptedNick = decryptionInfo.Decrypt(context.Message.NickName);

        var userNickName = await userOrchestrator.FindUserByNickNameAsync(decryptedNick);

        var isExist = !userNickName.Contains("not Found");
        
        var response = new ExistingUserResponse(userNickName, isExist);
        await context.RespondAsync(response);
    }
}