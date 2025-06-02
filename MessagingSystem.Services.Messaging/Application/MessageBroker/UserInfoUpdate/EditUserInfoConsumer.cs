using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;

public class EditUserInfoConsumer(
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IMessageOrchestrator messageOrchestrator,
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    ILogger<EditUserInfoConsumer> logger
        ) : IConsumer<EditUserInfoRequest>
{
    public async Task Consume(ConsumeContext<EditUserInfoRequest> context)
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
            decryptionInfo.DecryptObjectStrings(msg);

            var (memberTask, groupTask, messageTask) = (
                groupMemberOrchestrator.UpdateMemberInfoAsync(msg.UserHash, msg.UserNickName),
                groupInfoOrchestrator.EditGroupsAdminAsync(msg.UserHash, msg.UserNickName),
                messageOrchestrator.UpdateUserInfoInMessageAsync(msg.UserNickName, msg.UserHash)
            );

            await Task.WhenAll(memberTask, groupTask, messageTask);

            var results = new[] { memberTask.Result, groupTask.Result, messageTask.Result };

            foreach (var result in results.Where(r => !r.Success))
                logger.LogError("{Message}", result.Message);

            if (!string.IsNullOrWhiteSpace(memberTask.Result.Data))
                logger.LogInformation("Result: {Data}", memberTask.Result.Data);

            var response = new EditUserRollBack(results.All(r => r.Success));
            encryptionInfo.EncryptObjectStrings(response);
            encryptionInfo.EncryptRsaObjectStrings(response, publicKey);
            await context.RespondAsync(response);
        }
        catch (Exception ex)
        {
            var fallbackResponse = new DeleteUserInfoRollback("Unknown")
            {
                IsSuccess = false
            };
            await context.RespondAsync(fallbackResponse);
            logger.LogError(ex, "Unhandled exception in EditUserInfoConsumer");
        }
    }
}