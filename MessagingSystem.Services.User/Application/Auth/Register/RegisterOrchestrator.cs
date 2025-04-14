using AutoMapper;
using FluentValidation;
//using MassTransit;
//using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.Encrypt;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;

namespace MessagingSystem.Services.User.Application.Auth.Register;

public class RegisterOrchestrator(
    IUserRepository userRepository, 
    IMapper mapper, 
    IValidator<RegisterDto> validator,
    IHasherPassword hasherPassword,
    IEncryptInfo encryptInfo,
    IHasher hasher)
{
    public async Task<OperationResult<string>> RegisterUserAsync(RegisterDto registerDto)
    {
        var validationResult = await validator.ValidateAsync(registerDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        registerDto.Password = hasherPassword.Hash(registerDto.Password);
            
        var hashLogin = hasher.Hash(registerDto.Login);
        var hashEmail = hasher.Hash(registerDto.Email);
        var hashNickName = hasher.Hash(registerDto.NickName);
            
        var user = mapper.Map<Core.User.User>(registerDto);
        user.SetHashes(hashLogin, hashEmail, hashNickName);
            
        if (encryptInfo is EncryptInfo concreteEncryptor)
            concreteEncryptor.EncryptObjectStrings(user);
        
        try
        {
            await userRepository.AddUserAsync(user);

            if (user.UserName == null || user.Email == null) 
                return OperationResult<string>.Fail("User can not have null properties");
            
            //var confirmUserEmail = new ConfirmUserEmail(user.UserName, user.Email);
            //await publishEndpoint.Publish(confirmUserEmail);

            return OperationResult<string>.Ok("User registered and need to confirm email");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"User can not be added {e.Message}");
        }
    }
}