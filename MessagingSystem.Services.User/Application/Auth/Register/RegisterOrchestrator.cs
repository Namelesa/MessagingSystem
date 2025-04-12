using AutoMapper;
using FluentValidation;
using MessagingSystem.Services.User.Core.User;

namespace MessagingSystem.Services.User.Application.Auth.Register;

public class RegisterOrchestrator(IUserRepository userRepository, IMapper mapper, IValidator<RegisterDto> validator)
{
    public async Task<OperationResult<string>> RegisterUserAsync(RegisterDto registerDto)
    {
        // validation register dto 
        var validationResult = await validator.ValidateAsync(registerDto);
        if (!validationResult.IsValid) return OperationResult<string>.Fail(string.Join("; ", validationResult.Errors));
        
        try
        {
            var user = mapper.Map<Core.User.User>(registerDto);
            await userRepository.AddUserAsync(user);
            // send confirm email
            return OperationResult<string>.Ok("User registered and need to confirm email");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return OperationResult<string>.Fail($"User con not be added {e.Message}");
        }
    }
}