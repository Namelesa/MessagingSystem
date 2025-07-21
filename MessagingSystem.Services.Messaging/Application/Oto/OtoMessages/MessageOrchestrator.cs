using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Cashing;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;

public class MessageOrchestrator(
    IOtoMessageRepository otoMessageRepository,
    IMapper mapper,
    IValidator<MessagesDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IHasher hasher,
    ICacheService cacheService
    )
    : MessageOrchestratorBase<Message, MessagesDto>(hasher, mapper, 
        encryptionInfo, decryptionInfo, createValidator,
        editValidator, otoMessageRepository), IMessageOrchestrator
{
    private readonly IHasher _hasher = hasher;

    public async Task<List<Message>> LoadChatHistory(string sender, string recipient, int skip, int take)
    {
        var hashSender = _hasher.Hash(sender);
        var hashRecipient = _hasher.Hash(recipient);

        var keyPair = new[] { hashSender, hashRecipient }.OrderBy(x => x).ToArray();
        var cacheKey = $"oto:{keyPair[0]}:{keyPair[1]}:history:{skip}:{take}";

        var cached = await cacheService.GetAsync<List<Message>>(cacheKey);
        if (cached != null)
            return cached;

        var result = await otoMessageRepository.GetMessageStoryAsync(hashSender, hashRecipient, skip, take);
        result = DecryptListOfMessage(result);

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(1));
        await cacheService.RemoveAsync(cacheKey);
        return result;
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
    
    protected override async Task InvalidateCacheAsync(Message message)
    {
        var participants = new[] { message.SenderHash, message.RecipientHash }.OrderBy(x => x).ToArray();
        var cacheKey = $"oto:{participants[0]}:{participants[1]}:history:100";

        await cacheService.RemoveAsync(cacheKey);
    }
}