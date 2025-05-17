using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Oto.OtoChats;

public class ChatOrchestrator(
    IChatRepository chatRepository,
    IHasher hasher,
    IDecryptionInfo decryptionInfo
    ) : IChatOrchestrator
{
    public async Task<List<string>?> GetChatsAsync(string currentUserName)
    {
        var name = hasher.Hash(currentUserName);
        var encryptResult = await chatRepository.GetChatsAsync(name);
        return encryptResult == null 
            ? [] 
            : encryptResult
            .Select(decryptionInfo.Decrypt)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}