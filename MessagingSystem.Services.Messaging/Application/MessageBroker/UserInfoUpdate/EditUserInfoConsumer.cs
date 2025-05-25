using Encryptor.Decryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;

public class EditUserInfoConsumer(
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IDecryptionInfo decryptionInfo,
    ILogger<EditUserInfoConsumer> logger
        ) : IConsumer<EditUserInfoRequest>
{
    public async Task Consume(ConsumeContext<EditUserInfoRequest> context)
    {
        decryptionInfo.DecryptRsaObjectStrings(context.Message);
        decryptionInfo.DecryptObjectStrings(context.Message);
        
        var result = await groupMemberOrchestrator.UpdateMemberInfoAsync(
            context.Message.UserHash,
            context.Message.UserNickName);

        var result1 =
            await groupInfoOrchestrator.EditGroupsAdminAsync(context.Message.UserHash, context.Message.UserNickName);

        if (!result.Success && !result1.Success)
        {
            logger.LogError(result.Message);
            logger.LogError(result1.Message);
        }
        
        logger.LogInformation(result.Data);
    }
}