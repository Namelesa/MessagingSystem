using AutoMapper;
using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoChats;

public class ChatOrchestrator(
    IChatRepository chatRepository,
    IHasher hasher,
    IMapper mapper,
    IDecryptionInfo decryptionInfo
    ) : IChatOrchestrator
{
    public async Task<List<ChatDto>?> GetChatsAsync(string currentUserName)
    {
        var encryptedChats = await chatRepository.GetChatsAsync(hasher.Hash(currentUserName));

        if (encryptedChats == null)
            return [];
        
        foreach (var chat in encryptedChats)
        {
            chat.NickName = decryptionInfo.Decrypt(chat.NickName);
        }
        var result = mapper.Map<List<ChatDto>>(encryptedChats);
        return result;
    }
}