using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;

namespace MessagingSystem.Services.User.Application.User;

public class UserOrchestrator(
    IMapper mapper, 
    IValidator<UserDto> validator, 
    IUserRepository userRepository,
    IEncryptionInfo encryptInfo,
    IDecryptionInfo decryptionInfo,
    IHasher hasher,
    IPublishEndpoint publishEndpoint,
    IRequestClient<EditUserInfoRequest> client,
    IPublicKeyStorage publicKeyStorage) : IUserOrchestrator
{
    public async Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string userId)
    {
        var publicKeyNotification = GetPublicKeyNotification();
        var publicKeyMessaging = GetPublicKeyMessaging();
        
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));

        var existingUser = await userRepository.FindUserByIdAsync(userId);
        if (existingUser == null || publicKeyNotification == null || publicKeyMessaging == null)
            return OperationResult<string>.Fail("User not found");

        var oldHashNick = existingUser.HashNickName;
        var hashLogin = hasher.Hash(userDto.Login);
        var hashEmail = hasher.Hash(userDto.Email);
        var hashNickName = hasher.Hash(userDto.NickName);
        
        mapper.Map(userDto, existingUser);
        existingUser.SetHashes(hashLogin, hashEmail, hashNickName);
        
        encryptInfo.EncryptObjectStringsForUpdate(existingUser);
        
        if (existingUser.UserName == null 
            || existingUser.Email == null 
            || existingUser.HashNickName == null
            || oldHashNick == null) 
            return OperationResult<string>.Fail("User can not have null properties");
        
        try
        {
            var updateUserChats = new EditUserInfoRequest(encryptInfo.Encrypt(oldHashNick), existingUser.NickName);
            encryptInfo.EncryptRsaObjectStrings(updateUserChats, publicKeyMessaging);
        
            var response = await client.GetResponse<EditUserRollBack>(
                updateUserChats);
            
            decryptionInfo.DecryptRsaObjectStrings(response);
            decryptionInfo.DecryptObjectStrings(response);

            if (!response.Message.IsSuccess) 
                return OperationResult<string>.Fail("Can't update user info");
            
            await userRepository.UpdateUserAsync(existingUser);
            
            var editUserInfo = new EditUserEmail(existingUser.Email, existingUser.UserName);
            encryptInfo.EncryptRsaObjectStrings(editUserInfo, publicKeyNotification);
            await publishEndpoint.Publish(editUserInfo);
            
            return OperationResult<string>.Ok("Update user info");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Can not update user info {e}");
        }
    }
    public async Task<OperationResult<string>> DeleteUserAsync(string userId)
    {
        var publicKeyNotification = GetPublicKeyNotification();
        var user = await userRepository.FindUserByIdAsync(userId);
        if (user == null || publicKeyNotification == null)
            return OperationResult<string>.Fail("Not found user");
        
        try
        {
            if (user.UserName == null || user.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            await userRepository.DeleteUserAsync(user);
            
            var editUserInfo = new DeleteUserEmail(user.Email, user.UserName);
            encryptInfo.EncryptRsaObjectStrings(editUserInfo, publicKeyNotification);
            await publishEndpoint.Publish(editUserInfo);
            
            return OperationResult<string>.Ok("Delete user");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Can not delete user {e}");
        }
    }
    public async Task<string> FindUserByNickNameAsync(string nickName)
    {
        var existingUser = await userRepository.FindUserByHashNickNameAsync(nickName);
        
        return existingUser == null 
            ? "User not Found" 
            : existingUser.NickName;
    }
    private string? GetPublicKeyNotification()
        => publicKeyStorage.Get("Notification");
    private string? GetPublicKeyMessaging()
        => publicKeyStorage.Get("Messaging");
}