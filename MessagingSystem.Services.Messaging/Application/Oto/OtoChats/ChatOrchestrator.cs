using AutoMapper;
using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Caching;
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
        {
            chat.NickName = decryptionInfo.Decrypt(chat.NickName);
            chat.Image = decryptionInfo.Decrypt(chat.Image);
        }
        
        var distinctChats = encryptedChats
            .GroupBy(c => c.NickName)
            .Select(g => g.First())
            .ToList();

        var result = mapper.Map<List<ChatDto>>(distinctChats).ToList();
        
        await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
    
    public async Task InvalidateUserChatsCacheAsync(string nickName)
    {
        await cacheService.RemoveAsync($"user_chats:{nickName}");
    }
}