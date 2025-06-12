using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;

public class MessageOrchestrator(
    IOtoMessageRepository otoMessageRepository,
    IMapper mapper,
    IValidator<MessagesDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IHasher hasher)
    : MessageOrchestratorBase<Message, MessagesDto>(hasher, mapper, 
        encryptionInfo, decryptionInfo, createValidator,
        editValidator, otoMessageRepository), IMessageOrchestrator
{
    private readonly IHasher _hasher = hasher;

    public async Task<List<Message>> LoadChatHistory(string sender, string recipient, int take)
    {
        var hashSender = _hasher.Hash(sender);
        var hashRecipient = _hasher.Hash(recipient);
        
        var result =  await otoMessageRepository.GetMessageStoryAsync(hashSender, hashRecipient, take);
        return DecryptListOfMessage(result);
    }

    protected override void ApplyHashAndSet(MessagesDto dto, Message message)
    {
        var hashSender = _hasher.Hash(dto.Sender);
        var hashRecipient = _hasher.Hash(dto.Recipient);
        message.SetHashes(hashSender, hashRecipient);
    }

    protected override void EditMessage(Message message, EditMessageDto editDto)
    {
        message.EditInfo(editDto.Content);
    }
}