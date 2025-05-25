using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;

public class EditUserInfoConsumer(
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    ILogger<EditUserInfoConsumer> logger
        ) : IConsumer<EditUserInfoRequest>
{
    public async Task Consume(ConsumeContext<EditUserInfoRequest> context)
    {
        var publicKey = publicKeyStorage.Get("User");

        if (publicKey == null)
        {
            Console.WriteLine("Key not founded");
            return;
        }
        
        decryptionInfo.DecryptRsaObjectStrings(context.Message);
        decryptionInfo.DecryptObjectStrings(context.Message);
        
        var result = await groupMemberOrchestrator.UpdateMemberInfoAsync(
            context.Message.UserHash,
            context.Message.UserNickName);

        var result1 =
            await groupInfoOrchestrator.EditGroupsAdminAsync(context.Message.UserHash, context.Message.UserNickName);

        var isSuccess = true;
        
        if (!result.Success && !result1.Success)
        {
            logger.LogError(result.Message);
            logger.LogError(result1.Message);
            isSuccess = false;
        }
        
        var userRequest = new EditUserRollBack(isSuccess);
        encryptionInfo.EncryptObjectStrings(userRequest);
        encryptionInfo.EncryptRsaObjectStrings(userRequest, publicKey);
        await context.RespondAsync(userRequest);
        
        logger.LogInformation(result.Data);
    }
}