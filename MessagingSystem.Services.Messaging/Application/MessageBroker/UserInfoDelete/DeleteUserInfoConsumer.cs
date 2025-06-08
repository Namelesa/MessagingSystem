using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;

public class DeleteUserInfoConsumer(
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    ILogger<DeleteUserInfoConsumer> logger,
    IMessageOrchestrator messageOrchestrator,
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupMessagesOrchestrator groupMessagesOrchestrator
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
            
            var (memberTask, messageTask, groupMessagesTask) = (
                groupMemberOrchestrator.DeleteMemberInfoAsync(nickName),
                messageOrchestrator.DeleteUserInfoInMessageAsync(nickName),
                groupMessagesOrchestrator.DeleteUserInfoInMessageAsync(nickName)
            );

            await Task.WhenAll(memberTask, messageTask, groupMessagesTask);

            var results = new[] { memberTask.Result, messageTask.Result, groupMessagesTask.Result };

            foreach (var result in results.Where(r => !r.Success))
                logger.LogError("{Message}", result.Message);

            if (!string.IsNullOrWhiteSpace(memberTask.Result.Data))
                logger.LogInformation("Result: {Data}", memberTask.Result.Data);

            var isSuccess = results.All(r => r.Success);

            var response = new DeleteUserInfoRollback(nickName)
            {
                IsSuccess = isSuccess
            };
            
            encryptionInfo.EncryptObjectStrings(response);
            encryptionInfo.EncryptRsaObjectStrings(response, publicKey);
            await context.RespondAsync(response);
        }
        catch (Exception e)
        { 
            var fallbackResponse = new DeleteUserInfoRollback("Unknown")
            {
                IsSuccess = false
            };
            await context.RespondAsync(fallbackResponse);
            logger.LogError(e, "Unhandled exception in DeleteUserInfoConsumer");
        }
    }
}