using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;

namespace MessagingSystem.Services.User.Application.Messaging.UserChecker;

public class UsersCheckerConsumer(
    IDecryptionInfo decryptionInfo,
    IUserOrchestrator userOrchestrator,
    IPublicKeyStorage publicKeyStorage,
    IEncryptionInfo encryptionInfo,
    IHasher hasher) : IConsumer<ExistingUsersRequest>
{
    public async Task Consume(ConsumeContext<ExistingUsersRequest> context)
    {
        var publicKey = publicKeyStorage.Get("Messaging");

        if (publicKey == null)
        {
            Console.WriteLine("Key not found");
            return;
        }

        var responseList = new List<ExistingUserDto>();

        foreach (var encryptedNickRsa in context.Message.NickNames)
        {
            try
            {
                var decryptedNick = decryptionInfo.Decrypt(decryptionInfo.DecryptRsa(encryptedNickRsa));
                var nickHash = hasher.Hash(decryptedNick);

                var userResult = await userOrchestrator.FindUserByNickNameAsync(nickHash);

                if (userResult.Data != null)
                {
                    var plainNick = decryptionInfo.Decrypt(userResult.Data.UserNickName);
                    var plainImage = decryptionInfo.Decrypt(userResult.Data.Image);

                    var dto = new ExistingUserDto(plainNick, true, plainImage);
                    encryptionInfo.EncryptObjectStrings(dto);
                    dto.NickName = encryptionInfo.EncryptRsa(dto.NickName, publicKey);
                    dto.Image = encryptionInfo.EncryptRsa(dto.Image, publicKey);

                    responseList.Add(dto);
                }
                else
                {
                    responseList.Add(new ExistingUserDto("", false, ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User check error: {ex.Message}");
                responseList.Add(new ExistingUserDto("", false, ""));
            }
        }

        var response = new ExistingUsersResponse(responseList);
        await context.RespondAsync(response);
    }
}
