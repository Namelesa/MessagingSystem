using System.Security.Claims;
using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
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
    IRequestClient<EditUserInfoRequest> editClient,
    IRequestClient<DeleteUserInfoRequest> deleteClient,
    IPublicKeyStorage publicKeyStorage,
    IImageLoaderService imageLoaderService,
    IHttpContextAccessor httpContextAccessor) : IUserOrchestrator
{
    public async Task<OperationResult<UserDto>> GetUserInfoAsync(string nickName)
    {
        var currentNickName = httpContextAccessor.HttpContext?.User
            .Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;

        if (!string.Equals(currentNickName, nickName, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<UserDto>.Fail("Access denied");
        }

        var user = await userRepository.FindUserByHashNickNameAsync(hasher.Hash(nickName));
        if (user == null) 
            return OperationResult<UserDto>.Fail("User not found");
        
        decryptionInfo.DecryptObjectStrings(user);
    
        var userDto = mapper.Map<UserDto>(user);
    
        return OperationResult<UserDto>.Ok(userDto);
    }
    public async Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string nickName)
    {
        var publicKeyNotification = GetPublicKey("Notification");
        var publicKeyMessaging = GetPublicKey("Messaging");
        
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));

        var existingUser = await userRepository.FindUserByHashNickNameAsync(hasher.Hash(nickName));
        if (existingUser == null || publicKeyNotification == null || publicKeyMessaging == null)
            return OperationResult<string>.Fail("User not found");
        
        var currentNickName = httpContextAccessor.HttpContext?.User
            .Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;

        if (currentNickName != nickName)
        {
            return OperationResult<string>.Fail("Access denied");
        }

        var oldHashNick = existingUser.HashNickName;
        var hashLogin = hasher.Hash(userDto.Login);
        var hashEmail = hasher.Hash(userDto.Email);
        var hashNickName = hasher.Hash(userDto.NickName);
        
        if (existingUser.Image != null)
            await DeleteImage(decryptionInfo.Decrypt(existingUser.Image));
        
        mapper.Map(userDto, existingUser);
        existingUser.SetHashes(hashLogin, hashEmail, hashNickName);
        
        encryptInfo.EncryptObjectStringsForUpdate(existingUser);
        
        if (existingUser.UserName == null 
            || existingUser.Email == null 
            || existingUser.HashNickName == null
            || existingUser.Image == null
            || oldHashNick == null) 
            return OperationResult<string>.Fail("User can not have null properties");
        
        try
        {
            var updateUserChats = new EditUserInfoRequest(encryptInfo.Encrypt(oldHashNick), existingUser.NickName, existingUser.Image);
            encryptInfo.EncryptRsaObjectStrings(updateUserChats, publicKeyMessaging);
        
            var response = await editClient.GetResponse<EditUserRollBack>(
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
            return OperationResult<string>.Fail($"Can not update user info {e}");
        }
    }
    public async Task<OperationResult<string>> DeleteUserAsync(string nickName)
    {
        var publicKeyNotification = GetPublicKey("Notification");
        var publicKeyMessaging = GetPublicKey("Messaging");
        
        var user = await userRepository.FindUserByHashNickNameAsync(nickName);
        if (user == null || publicKeyNotification == null || publicKeyMessaging == null)
            return OperationResult<string>.Fail("Not found user");
        
        var currentNickName = httpContextAccessor.HttpContext?.User
            .Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;

        if (!string.Equals(currentNickName, nickName, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<string>.Fail("Access denied");
        }
        
        try
        {
            if (user.UserName == null || user.Email == null || user.HashNickName == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            var deleteUserChats = new DeleteUserInfoRequest(encryptInfo.Encrypt(user.HashNickName));
            encryptInfo.EncryptRsaObjectStrings(deleteUserChats, publicKeyMessaging);
        
            var response = await deleteClient.GetResponse<DeleteUserInfoRollback>(deleteUserChats);
            
            decryptionInfo.DecryptRsaObjectStrings(response);
            decryptionInfo.DecryptObjectStrings(response);

            if (!response.Message.IsSuccess) 
                return OperationResult<string>.Fail("Can't delete user info");
            
            if (user.Image != null)
                await DeleteImage(decryptionInfo.Decrypt(user.Image));
            
            await userRepository.DeleteUserAsync(user);
            
            var editUserInfo = new DeleteUserEmail(user.Email, user.UserName);
            encryptInfo.EncryptRsaObjectStrings(editUserInfo, publicKeyNotification);
            await publishEndpoint.Publish(editUserInfo);
            
            return OperationResult<string>.Ok("Delete user");
        }
        catch (Exception e)
        {
            return OperationResult<string>.Fail($"Can not delete user {e}");
        }
    }
    public async Task<OperationResult<UserFoundDto>> FindUserByNickNameAsync(string nickName)
    {
        var existingUser = await userRepository.FindUserByHashNickNameAsync(nickName);
        
        return existingUser == null 
            ? OperationResult<UserFoundDto>.Fail("User not found") 
            : OperationResult<UserFoundDto>.Ok(new UserFoundDto(existingUser.NickName, existingUser.Image));
    }
    public async Task<OperationResult<List<UserFoundDto>>> FindUsersByNickNamesAsync(List<string> hashNickNames)
    {
        var users = await userRepository.FindUsersByHashNickNamesAsync(hashNickNames);
        
        return users == null 
            ? OperationResult<List<UserFoundDto>>.Fail("Users not found") 
            : OperationResult<List<UserFoundDto>>.Ok(users
                .Select(u => new UserFoundDto(u.NickName, u.Image)).ToList());
    }
    private string? GetPublicKey(string key) 
        => publicKeyStorage.Get(key);
    private async Task DeleteImage(string image)
    {    
        if (string.IsNullOrEmpty(image))
            return;
        
        var uri = new Uri(image);
        var path = uri.AbsolutePath.TrimStart('/');

        var segments = path.Split('/', 2);
        var key = segments.Length == 2 ? segments[1] : segments[0];
            
        await imageLoaderService.DeleteAsync(key);
    }
}