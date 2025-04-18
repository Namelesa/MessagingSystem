using Encryptor.Decryption;
using FluentValidation;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Jwt;

namespace MessagingSystem.Services.User.Application.Auth.Login;

public class LoginOrchestrator(
    IUserRepository userRepository, 
    IValidator<LoginDto> validator,
    IDecryptionInfo decryptionInfo,
    IJwtService jwtService,
    IHasher hasher) : ILoginOrchestrator
{
    public async Task<OperationResult<string>> LoginUserAsync(LoginDto loginDto)
    {
        var validationResult = await validator.ValidateAsync(loginDto);
        if (!validationResult.IsValid) 
            return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        var user = await userRepository.FindUserByHashLoginAsync(hasher.Hash(loginDto.Login));
        if (user == null) 
            return OperationResult<string>.Fail("User not found");
        
        if(!user.EmailConfirmed) return OperationResult<string>.Fail("Please confirm email");

        var res = user.PasswordHash != null &&
                  await jwtService.AuthenticateAndSetCookieAsync(loginDto, decryptionInfo.Decrypt(user.PasswordHash));
        
        return OperationResult<string>.Ok(res.ToString());
    }
}