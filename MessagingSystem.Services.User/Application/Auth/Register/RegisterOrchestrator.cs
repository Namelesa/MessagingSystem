using AutoMapper;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Add;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;

namespace MessagingSystem.Services.User.Application.Auth.Register;

public class RegisterOrchestrator(
    IUserRepository userRepository, 
    IMapper mapper, 
    IValidator<RegisterDto> validator,
    IHasherPassword hasherPassword,
    IEncryptionInfo encryptInfo,
    IHasher hasher,
    IPublishEndpoint publishEndpoint,
    IPublicKeyStorage publicKeyStorage,
    IRequestClient<AddUserRequest> client) : IRegisterOrchestrator
{
    public async Task<OperationResult<string>> RegisterUserAsync(RegisterDto registerDto)
    {
        var publicKeyNotification = GetPublicKey("Notification");
        var publicKeyMessaging = GetPublicKey("Messaging");
        
        if (publicKeyNotification == null || publicKeyMessaging == null)
            return OperationResult<string>.Fail("Public key for Notification or Messaging service not found");
        
        var validationResult = await validator.ValidateAsync(registerDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        registerDto.Password = hasherPassword.Hash(registerDto.Password);
            
        var hashLogin = hasher.Hash(registerDto.Login);
        var hashEmail = hasher.Hash(registerDto.Email);
        var hashNickName = hasher.Hash(registerDto.NickName);
            
        var user = mapper.Map<Core.User.User>(registerDto);
        user.SetHashes(hashLogin, hashEmail, hashNickName);
        
        encryptInfo.EncryptObjectStrings(user);
        
        try
        {
            if(user.HashNickName == null || user.Image == null || user.UserName == null || user.Email == null)
                return OperationResult<string>.Fail("User can not have null properties");
            
            var existingUserResponse = await client.GetResponse<AddUserResponse>(
                new AddUserRequest(encryptInfo.EncryptRsa(user.HashNickName, publicKeyMessaging), 
                    encryptInfo.EncryptRsa(user.Image, publicKeyMessaging)));
            
            if (!existingUserResponse.Message.Success)
                return OperationResult<string>.Fail("User already exists");
            
            await userRepository.AddUserAsync(user);
            
            var confirmUserEmail = new ConfirmUserEmail(user.UserName, user.Email, hashNickName);
            encryptInfo.EncryptRsaObjectStrings(confirmUserEmail, publicKeyNotification);
            
            await publishEndpoint.Publish(confirmUserEmail);

            return OperationResult<string>.Ok("User registered and need to confirm email");
        }
        catch (Exception e)
        {
            return OperationResult<string>.Fail($"User can not be added {e.Message}");
        }
    }
    
    public async Task<OperationResult<string>> ConfirmEmailAsync(string hashNickName)
    {
        var decodedHash = Uri.UnescapeDataString(hashNickName);
        var user = await userRepository.FindUserByHashNickNameAsync(decodedHash);

        if (user == null)
            return OperationResult<string>.Fail("User not found");

        user.EmailConfirmed = true;

        try
        {
            await userRepository.UpdateUserAsync(user);
            return OperationResult<string>.Ok("User confirm email");
        }
        catch (Exception e)
        {
            return OperationResult<string>.Fail($"Error {e}");
        }
    }
    private string? GetPublicKey(string key) 
        => publicKeyStorage.Get(key);
}