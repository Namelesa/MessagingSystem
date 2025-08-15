using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Oto;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;

public class DeleteUserInfoConsumer(
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    ILogger<DeleteUserInfoConsumer> logger,
    IMessageOrchestrator messageOrchestrator,
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupMessagesOrchestrator groupMessagesOrchestrator,
    IHubContext<GroupChatHub> groupHubContext, 
    IHubContext<OtoChatHub> otoHubContext,
    IUserOrchestrator userOrchestrator
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
            var nickNameHash = decryptionInfo.Decrypt(msg.UserNickNameHash);
            var nickNameEncrypt = decryptionInfo.Decrypt(msg.UserNickName);
            var nickName = decryptionInfo.Decrypt(nickNameEncrypt);
            
            var (memberTask, messageTask, groupMessagesTask, userImageTask) = (
                groupMemberOrchestrator.DeleteMemberInfoAsync(nickNameHash),
                messageOrchestrator.DeleteUserInfoInMessageAsync(nickNameHash),
                groupMessagesOrchestrator.DeleteUserInfoInMessageAsync(nickNameHash),
                userOrchestrator.DeleteUserAsync(nickNameHash)
            );

            await Task.WhenAll(memberTask, messageTask, groupMessagesTask, userImageTask);

            var results = new dynamic[] 
            { 
                memberTask.Result, 
                messageTask.Result, 
                groupMessagesTask.Result, 
                userImageTask.Result 
            };

            var allSuccessful = true;

            foreach (var result in results)
            {
                if (result.Success) continue;
                allSuccessful = false;
            }
            
            if (allSuccessful)
            {
                var groupIds = memberTask.Result.Data ?? [];
                await NotifyUserInfoChanged(nickName, groupIds);
            }

            var response = new DeleteUserInfoRollback(nickNameHash)
            {
                IsSuccess = allSuccessful
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
    
    private async Task NotifyUserInfoChanged(
        string userName,
        List<Guid> groupIds)
    {
        try
        {
            var notification = new
            {
                UserName = userName
            };
            
            foreach (var groupId in groupIds)
            {
                await groupHubContext.Clients.Group(groupId.ToString())
                    .SendAsync("UserInfoDeleted", notification);
                    
                logger.LogDebug("Sent UserInfoChanged notification to group: {GroupId}", groupId);
            }
            await otoHubContext.Clients.All.SendAsync("UserInfoDeleted", notification);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify user info delete for user: {UserName}", userName);
            throw;
        }
    }
}