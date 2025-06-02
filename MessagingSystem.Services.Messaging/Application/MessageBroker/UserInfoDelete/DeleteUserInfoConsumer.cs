using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;

public class DeleteUserInfoConsumer(
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    ILogger<DeleteUserInfoConsumer> logger,
    IMessageOrchestrator messageOrchestrator,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IGroupMemberOrchestrator groupMemberOrchestrator
    ) : IConsumer<DeleteUserInfoRequest>
{
    public async Task Consume(ConsumeContext<DeleteUserInfoRequest> context)
    {
        try
        {
            var publicKey = publicKeyStorage.Get("User");
            if (publicKey is null)
            {
                logger.LogWarning("Public key for 'User' not found");
                return;
            }

            var msg = context.Message;

            decryptionInfo.DecryptRsaObjectStrings(msg);
            var nickName = decryptionInfo.Decrypt(msg.UserNickNameHash);
            
            var (memberTask, messageTask) = (
                groupMemberOrchestrator.DeleteMemberInfoAsync(nickName),
                //groupInfoOrchestrator.EditGroupsAdminAsync(nickName),
                messageOrchestrator.DeleteUserInfoInMessageAsync(nickName)
            );

            await Task.WhenAll(memberTask, messageTask);

            var results = new[] { memberTask.Result, messageTask.Result };

            foreach (var result in results.Where(r => !r.Success))
                logger.LogError("{Message}", result.Message);

            if (!string.IsNullOrWhiteSpace(memberTask.Result.Data))
                logger.LogInformation("Result: {Data}", memberTask.Result.Data);

            var response = new DeleteUserInfoRollback(nickName)
            {
                IsSuccess = true
            };
            encryptionInfo.EncryptObjectStrings(response);
            encryptionInfo.EncryptRsaObjectStrings(response, publicKey);
            await context.RespondAsync(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception in DeleteUserInfoConsumer");
        }
    }
}