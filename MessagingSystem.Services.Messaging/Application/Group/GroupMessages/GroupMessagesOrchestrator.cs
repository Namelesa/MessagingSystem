using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMessages;

public class GroupMessagesOrchestrator(
    IGroupMessagesRepository messageRepository,
    IMapper mapper,
    IHasher hasher,
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IValidator<GroupMessageDto> createValidator
    ) : IGroupMessagesOrchestrator
{
    public async Task<OperationResult<CreatedMessageResult>> SendMessageAsync(GroupMessageDto messagesDto)
    {
        var validationResult = await createValidator.ValidateAsync(messagesDto);
        if (!validationResult.IsValid) 
            return OperationResult<CreatedMessageResult>.Fail(string.Join("; ", validationResult.Errors));
        
        var hashSender = hasher.Hash(messagesDto.Sender);
        
        var message = mapper.Map<GroupMessage>(messagesDto);
        message.SetHashes(hashSender);
        
        encryptionInfo.EncryptObjectStrings(message);
        var result = await messageRepository.CreateMessageAsync(message);
        
        return result != null
            ? OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(result.Id, result.SendTime))
            : OperationResult<CreatedMessageResult>.Fail("Failed to send message");
    }
    public async Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);
        
        if(message == null)
            return OperationResult<string>.Fail("Message not found");
        
        message.EditInfo(messagesDto.Content);
        encryptionInfo.EncryptObjectStrings(message);
        var result = await messageRepository.EditMessageAsync(message);
        
        return OperationResult<string>.Ok(result.Sender);
    }
    public async Task<OperationResult<string>> SoftDeleteMessageAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<string>.Fail("Message was not founded");
        
        var result = await messageRepository.SoftDeleteMessageAsync(message);
        
        return OperationResult<string>.Ok(result.Sender);
    }
    public async Task<OperationResult<string>> DeleteMessageAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<string>.Fail("Message was not founded");
        
        var result = await messageRepository.DeleteMessageAsync(message);
        
        return OperationResult<string>.Ok(result.Sender);
    }
    public async Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        return message == null 
            ? OperationResult<string>.Fail("Message was not founded") 
            : OperationResult<string>.Ok(message.Sender);
    }
    public async Task<List<GroupMessage>> LoadChatHistory(Guid groupId, int take)
    {
        var result =  await messageRepository.GetMessageStoryAsync(groupId, take);
        return DecryptListOfMessage(result);
    }
    public async Task<OperationResult<GroupMessage>> ReplyForMessageAsync(Guid messageId, Guid replyId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<GroupMessage>.Fail("Can not find message");

        var reply = await messageRepository.ReplyMessageAsync(replyId, message);
        decryptionInfo.DecryptObjectStrings(reply);
        
        return OperationResult<GroupMessage>.Ok(reply);
    }
    public async Task<List<GroupMessage>?> FindMessagesAsync(MessageFilter messageFilter)
    {
        var filterCopy = new MessageFilter
        {
            Sender = messageFilter.Sender != null ? hasher.Hash(messageFilter.Sender) : null,
            Date = messageFilter.Date
        };

        var result = await messageRepository.FindMessagesAsync(filterCopy);
        return result == null ? [] : DecryptListOfMessage(result);
    }
    public async Task<OperationResult<string>> UpdateUserInfoInMessageAsync(string newNickName, string oldUserHashName)
    {
        var newUserHash = hasher.Hash(newNickName);
        var newEncryptedNickName = encryptionInfo.Encrypt(newNickName);
        try
        {
            var affectedRows = await messageRepository.UpdateUserHashesAsync(oldUserHashName, newEncryptedNickName, newUserHash);
            return affectedRows >= 0 
                ? OperationResult<string>.Ok($"Update successful. Rows affected: {affectedRows}") 
                : OperationResult<string>.Fail("No rows were updated. Possibly invalid user hash.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Exception occurred: {e.Message}");
        }
    }
    public async Task<OperationResult<string>> DeleteUserInfoInMessageAsync(string userHash)
    {
        try
        {
            var result = await messageRepository.DeleteUserHashesAsync(userHash);
            return result >= 0 
                ? OperationResult<string>.Ok($"Delete successful. Rows affected: {result}") 
                : OperationResult<string>.Fail("No rows were deleted. Possibly invalid user hash.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Exception occurred: {e.Message}");
        }
    }
    private List<GroupMessage> DecryptListOfMessage(List<GroupMessage> encryptedMessages)
    {
        encryptedMessages.ForEach(decryptionInfo.DecryptObjectStrings);
        return encryptedMessages;
    }
}