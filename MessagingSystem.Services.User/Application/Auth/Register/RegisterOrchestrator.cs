using AutoMapper;
using FluentValidation;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.Encrypt;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;

namespace MessagingSystem.Services.User.Application.Auth.Register;

public class RegisterOrchestrator(
    IUserRepository userRepository, 
    IMapper mapper, 
    IValidator<RegisterDto> validator,
    IHasherPassword hasherPassword,
    IEncryptInfo encryptInfo)
{
    public async Task<OperationResult<string>> RegisterUserAsync(RegisterDto registerDto)
    {
        var validationResult = await validator.ValidateAsync(registerDto);
        if (!validationResult.IsValid) return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        try
        {
            registerDto.Password = hasherPassword.Hash(registerDto.Password);
            var user = mapper.Map<Core.User.User>(registerDto);
            if (encryptInfo is EncryptInfo concreteEncryptor)
                concreteEncryptor.EncryptObjectStrings(user);
            
            await userRepository.AddUserAsync(user);
            // send confirm email
            return OperationResult<string>.Ok("User registered and need to confirm email");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"User can not be added {e.Message}");
        }
    }
    
}