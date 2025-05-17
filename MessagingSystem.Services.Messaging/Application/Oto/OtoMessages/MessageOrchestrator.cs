using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;

public class MessageOrchestrator(
    IMessageRepository messageRepository, 
    IMapper mapper,
    IValidator<MessagesDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IHasher hasher
    ) : IMessageOrchestrator
{
    public async Task<OperationResult<CreatedMessageResult>> SendMessageAsync(MessagesDto messagesDto)
    {
        var validationResult = await createValidator.ValidateAsync(messagesDto);
        if (!validationResult.IsValid) 
            return OperationResult<CreatedMessageResult>.Fail(string.Join("; ", validationResult.Errors));
        
        var hashSender = hasher.Hash(messagesDto.Sender);
        var hashRecipient = hasher.Hash(messagesDto.Recipient);
        
        var message = mapper.Map<Message>(messagesDto);
        message.SetHashes(hashSender, hashRecipient);
        
        encryptionInfo.EncryptObjectStrings(message);
        var result = await messageRepository.CreateMessageAsync(message);
        
        return result != null
            ? OperationResult<CreatedMessageResult>.Ok(new CreatedMessageResult(result.Id, result.Date))
            : OperationResult<CreatedMessageResult>.Fail("Failed to send message");
    }
    public async Task<OperationResult<string>> EditMessageAsync(Guid messageId, EditMessageDto messagesDto)
    {
        var validationResult = await editValidator.ValidateAsync(messagesDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<string>.Fail("Message was not founded");
        
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
    public async Task<OperationResult<Message>> ReplyForMessageAsync(Guid messageId, Guid replyId)
    {
        var message = await messageRepository.FindMessageByIdAsync(messageId);

        if (message == null)
            return OperationResult<Message>.Fail("Can not find message");

        var reply = await messageRepository.ReplyMessageAsync(replyId, message);
        decryptionInfo.DecryptObjectStrings(reply);
        
        return OperationResult<Message>.Ok(reply);
    }
    public async Task<List<Message>> LoadChatHistory(string sender, string recipient, int take)
    {
        var hashSender = hasher.Hash(sender);
        var hashRecipient = hasher.Hash(recipient);
        
        var result =  await messageRepository.GetMessageStoryAsync(hashSender, hashRecipient, take);
        return DecryptListOfMessage(result);
    }
    public async Task<List<Message>?> FindMessagesAsync(MessageFilter messageFilter)
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
    private List<Message> DecryptListOfMessage(List<Message> encryptedMessages)
    {
        encryptedMessages.ForEach(decryptionInfo.DecryptObjectStrings);
        return encryptedMessages;
    }
}