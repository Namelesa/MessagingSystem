using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Oto;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;

namespace MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;

public class EditUserInfoConsumer(
    IGroupMemberOrchestrator groupMemberOrchestrator,
    IGroupMessagesOrchestrator groupMessagesOrchestrator,
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IUserOrchestrator userOrchestrator,
    IMessageOrchestrator messageOrchestrator,
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IPublicKeyStorage publicKeyStorage,
    IHubContext<GroupChatHub> groupHubContext, 
    IHubContext<OtoChatHub> otoHubContext, 
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
        
            var (memberTask, groupTask, messageTask, groupMessageTask, userImageTask) = (
                groupMemberOrchestrator.UpdateMemberInfoAsync(msg.UserHash, msg.UserNickName, msg.Image),
                groupInfoOrchestrator.EditGroupsAdminAsync(msg.UserHash, msg.UserNickName),
                messageOrchestrator.UpdateUserInfoInMessageAsync(msg.UserNickName, msg.UserHash),
                groupMessagesOrchestrator.UpdateUserInfoInMessageAsync(msg.UserNickName, msg.UserHash),
                userOrchestrator.UpdateUserAsync(msg.UserNickName, msg.UserHash, encryptionInfo.Encrypt(msg.Image))
            );

            await Task.WhenAll(memberTask, groupTask, messageTask, groupMessageTask, userImageTask);
        
            var results = new dynamic[]
            {
                memberTask.Result,
                groupTask.Result,
                messageTask.Result,
                groupMessageTask.Result,
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
                await NotifyUserInfoChanged(msg.UserNickName, groupIds, msg.Image, msg.OldNickName);
            }
        
            var response = new EditUserRollBack(allSuccessful);
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

    private async Task NotifyUserInfoChanged(
        string userName, 
        List<Guid> groupIds, 
        string image, 
        string oldNickName)
    {
        try
        {
            var notification = new
            {
                NewUserName = userName,
                Image = image,
                OldNickName = oldNickName,
                UpdatedAt = DateTime.UtcNow.ToString("O") 
            };
            
            logger.LogInformation("Sending user info update notification for user: {UserName} to {GroupCount} groups", 
                userName, groupIds.Count);
            
            foreach (var groupId in groupIds)
            {
                await groupHubContext.Clients.Group(groupId.ToString())
                    .SendAsync("UserInfoChanged", notification);
                    
                logger.LogDebug("Sent UserInfoChanged notification to group: {GroupId}", groupId);
            }
            await otoHubContext.Clients.All.SendAsync("UserInfoChanged", notification);
            
            logger.LogInformation("Successfully sent UserInfoChanged notifications for user: {UserName}", userName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify user info change for user: {UserName}", userName);
            throw;
        }
    }
}