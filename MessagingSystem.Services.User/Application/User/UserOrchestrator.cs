using AutoMapper;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
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
    IHasher hasher,
    IPublishEndpoint publishEndpoint,
    IPublicKeyStorage publicKeyStorage) : IUserOrchestrator
{
    public async Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string userId)
    {
        var publicKey = GetPublicKey();
        
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));

        var existingUser = await userRepository.FindUserByIdAsync(userId);
        if (existingUser == null || publicKey == null)
            return OperationResult<string>.Fail("User not found");

        var hashLogin = hasher.Hash(userDto.Login);
        var hashEmail = hasher.Hash(userDto.Email);
        var hashNickName = hasher.Hash(userDto.NickName);
        
        mapper.Map(userDto, existingUser);
        existingUser.SetHashes(hashLogin, hashEmail, hashNickName);
        
        encryptInfo.EncryptObjectStringsForUpdate(existingUser);
        
        try
        {
            if (existingUser.UserName == null || existingUser.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            await userRepository.UpdateUserAsync(existingUser);
            
            var editUserInfo = new EditUserEmail(existingUser.Email, existingUser.UserName);
            encryptInfo.EncryptRsaObjectStrings(editUserInfo, publicKey);
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
        var publicKey = GetPublicKey();
        var user = await userRepository.FindUserByIdAsync(userId);
        if (user == null || publicKey == null)
            return OperationResult<string>.Fail("Not found user");
        
        try
        {
            if (user.UserName == null || user.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            await userRepository.DeleteUserAsync(user);
            
            var editUserInfo = new DeleteUserEmail(user.Email, user.UserName);
            encryptInfo.EncryptRsaObjectStrings(editUserInfo, publicKey);
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
    
    private string? GetPublicKey()
        => publicKeyStorage.Get("Notification");
}