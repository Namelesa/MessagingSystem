using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Messages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Messages;

public abstract class MessageOrchestratorBase<TMessage, TCreateDto>
    (IHasher hasher,
    IMapper mapper,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IValidator<TCreateDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IMessageRepository<TMessage> messageRepository
    )
    where TMessage : class, IMessageEntity
    where TCreateDto : class
{

    public async Task<OperationResult<CreatedMessageResult>> SendMessageAsync(TCreateDto messagesDto)
    {
        var validationResult = await createValidator.ValidateAsync(messagesDto);
        if (!validationResult.IsValid)
            return OperationResult<CreatedMessageResult>.Fail(string.Join("; ", validationResult.Errors));

        var message = mapper.Map<TMessage>(messagesDto);
        ApplyHashAndSet(messagesDto, message);
        await InvalidateCacheAsync(message);
        encryptionInfo.EncryptObjectStrings(message);
        var result = await messageRepository.CreateMessageAsync(message);

        return result != null
            ? OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(result.Id, result.SendTime))
            : OperationResult<CreatedMessageResult>.Fail("Failed to send message");
    }
    public async Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto)
    {
        var validationResult = await editValidator.ValidateAsync(messagesDto);
        if (!validationResult.IsValid)
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));

        var message = await messageRepository.FindMessageByIdAsync(messageId);
        if (message == null)
            return OperationResult<string>.Fail("Message not found");

        EditMessage(message, messagesDto);
        await InvalidateCacheAsync(message);
        encryptionInfo.EncryptObjectStrings(message);
        var result = await messageRepository.EditMessageAsync(message);

        return OperationResult<string>.Ok(result.Id.ToString());
    }
    public async Task<OperationResult<string>> DeleteMessageAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);
        if (message == null)
            return OperationResult<string>.Fail("Message not found");
        
        var result = await messageRepository.DeleteMessageAsync(message);
        await InvalidateCacheAsync(message);
        return OperationResult<string>.Ok(result.Id.ToString());
    }
    public async Task<OperationResult<string>> SoftDeleteMessageAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);
        if (message == null)
            return OperationResult<string>.Fail("Message not found");

        var result = await messageRepository.SoftDeleteMessageAsync(message);
        await InvalidateCacheAsync(message);
        return OperationResult<string>.Ok(result.Id.ToString());
    }
    public async Task<OperationResult<string>> FindMessageByIdAsync(Guid messageId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);
        return message == null
            ? OperationResult<string>.Fail("Message not found")
            : OperationResult<string>.Ok(message.Id.ToString());
    }
    public async Task<OperationResult<TMessage>> ReplyForMessageAsync(Guid messageId, Guid replyId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<TMessage>.Fail("Can not find message");

        var reply = await messageRepository.ReplyMessageAsync(replyId, message);
        decryptionInfo.DecryptObjectStrings(reply);
        await InvalidateCacheAsync(message);
        return OperationResult<TMessage>.Ok(reply);
    }
    public async Task<OperationResult<string>> DeleteUserInfoInMessageAsync(string userHash)
    {
        try
        {
            var result = await messageRepository.DeleteUserHashesAsync(userHash);
            
            await InvalidateCacheByUserHashAsync(userHash);
            
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
    public async Task<OperationResult<string>> UpdateUserInfoInMessageAsync(string newNickName, string oldUserHashName)
    {
        var newUserHash = hasher.Hash(newNickName);
        var newEncryptedNickName = encryptionInfo.Encrypt(newNickName);
        try
        {
            var affectedRows = await messageRepository.UpdateUserHashesAsync(oldUserHashName, newEncryptedNickName, newUserHash);
            
            await InvalidateCacheByUserHashAsync(oldUserHashName);

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
    public async Task<List<TMessage>?> FindMessagesAsync(MessageFilter messageFilter)
    {
        var filterCopy = new MessageFilter
        {
            Sender = messageFilter.Sender != null ? hasher.Hash(messageFilter.Sender) : null,
            Recipient = messageFilter.Recipient != null ? hasher.Hash(messageFilter.Recipient) : null,
            Date = messageFilter.Date
        };

        var result = await messageRepository.FindMessagesAsync(filterCopy);
        return result == null ? [] : DecryptListOfMessage(result);
    }
    protected List<TMessage> DecryptListOfMessage(List<TMessage> encryptedMessages)
    {
        encryptedMessages.ForEach(decryptionInfo.DecryptObjectStrings);
        return encryptedMessages;
    }
    protected abstract Task InvalidateCacheAsync(TMessage message);
    protected abstract Task InvalidateCacheByUserHashAsync(string userHash);
    protected abstract void ApplyHashAndSet(TCreateDto dto, TMessage message);
    protected abstract void EditMessage(TMessage message, EditMessageDto editDto);
}