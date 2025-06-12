using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupMessages;

public class GroupMessagesOrchestrator(
    IMapper mapper,
    IHasher hasher,
    IDecryptionInfo decryptionInfo,
    IEncryptionInfo encryptionInfo,
    IValidator<GroupMessageDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IGroupMessagesRepository messageRepository
    ) : MessageOrchestratorBase<GroupMessage, GroupMessageDto>(hasher, mapper, 
    encryptionInfo, decryptionInfo, createValidator, editValidator, messageRepository), 
    IGroupMessagesOrchestrator
{
    private readonly IHasher _hasher = hasher;
    
    public async Task<List<GroupMessage>> LoadChatHistory(Guid groupId, int take)
    {
        var result =  await messageRepository.GetMessageStoryAsync(groupId, take);
        return DecryptListOfMessage(result);
    }

    protected override void ApplyHashAndSet(GroupMessageDto dto, GroupMessage message)
    {
        var hashSender = _hasher.Hash(dto.Sender);
        message.SetHashes(hashSender);
    }

    protected override void EditMessage(GroupMessage message, EditMessageDto editDto)
    {
        message.EditInfo(editDto.Content);
    }
}