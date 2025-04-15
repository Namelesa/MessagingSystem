using AutoMapper;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.Encrypt;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;

namespace MessagingSystem.Services.User.Application.User;

public class UserOrchestrator(
    IMapper mapper, 
    IValidator<UserDto> validator, 
    IUserRepository userRepository,
    IEncryptInfo encryptInfo,
    IHasher hasher,
    IPublishEndpoint publishEndpoint)
{
    public async Task<OperationResult<string>> EditUserInfoAsync(UserDto userDto, string userId)
    {
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));

        var existingUser = await userRepository.FindUserByIdAsync(userId);

        if (existingUser == null)
            return OperationResult<string>.Fail("User not found");

        var hashLogin = hasher.Hash(userDto.Login);
        var hashEmail = hasher.Hash(userDto.Email);
        var hashNickName = hasher.Hash(userDto.NickName);
        
        mapper.Map(userDto, existingUser);
        existingUser.SetHashes(hashLogin, hashEmail, hashNickName);
        
        if (encryptInfo is EncryptInfo concreteEncryptor)
            concreteEncryptor.EncryptObjectStringsForUpdate(existingUser);
        
        Console.WriteLine(existingUser.UserName);
        try
        {
            if (existingUser.UserName == null || existingUser.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            await userRepository.UpdateUserAsync(existingUser);
            
            var editUserInfo = new EditUserEmail(existingUser.Email, existingUser.UserName);
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
        var user = await userRepository.FindUserByIdAsync(userId);
        if (user == null)
            return OperationResult<string>.Fail("Not found user");
        
        try
        {
            if (user.UserName == null || user.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            await userRepository.DeleteUserAsync(user);
            
            var editUserInfo = new DeleteUserEmail(user.Email, user.UserName);
            await publishEndpoint.Publish(editUserInfo);
            
            return OperationResult<string>.Ok("Delete user");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"Can not delete user {e}");
        }
    }
}