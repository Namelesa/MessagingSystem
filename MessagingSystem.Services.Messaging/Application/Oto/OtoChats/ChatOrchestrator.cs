using AutoMapper;
using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Cashing;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoChats;

public class ChatOrchestrator(
    IChatRepository chatRepository,
    IHasher hasher,
    IMapper mapper,
    IDecryptionInfo decryptionInfo,
    ICacheService cacheService
) : IChatOrchestrator
{
    public async Task<List<ChatDto>?> GetChatsAsync(string currentUserName)
    {
        var cacheKey = $"user_chats:{currentUserName}";
        var cached = await cacheService.GetAsync<List<ChatDto>>(cacheKey);
        if (cached != null)
            return cached;

        var encryptedChats = await chatRepository.GetChatsAsync(hasher.Hash(currentUserName));
        if (encryptedChats == null)
            return [];

        foreach (var chat in encryptedChats)
            (chat.NickName, chat.Image) = (decryptionInfo.Decrypt(chat.NickName), decryptionInfo.Decrypt(chat.Image));

        var result = mapper.Map<List<ChatDto>>(encryptedChats);

        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }
}