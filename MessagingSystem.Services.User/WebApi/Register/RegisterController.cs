using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.WebApi.Register.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.User.WebApi.Register;

[ApiController]
[Route("api/auth")]
public class RegisterController(
    IRegisterOrchestrator registerOrchestrator, 
    IMapper mapper,
    IImageLoaderService imageLoaderService) : ControllerBase
{
    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RegisterAsync([Required, FromForm] RegisterContract registerContract)
    {
        if (registerContract.Image is { Length: > 0 })
        {
            await using var stream = registerContract.Image.OpenReadStream();
            var fileName = $"{Guid.NewGuid()}.jpg";
            var url = await imageLoaderService.UploadOrReplaceAsync(stream, fileName);
            registerContract.AvatarUrl = url;
        }

        var registerDto = mapper.Map<RegisterDto>(registerContract);
        registerDto.Image = registerContract.AvatarUrl;
        var result = await registerOrchestrator.RegisterUserAsync(registerDto);

        return result.Success
            ? Ok(new { message = result.Data })
            : BadRequest(new { message = result.Message });
    } 
    
    [HttpGet("confirm-email")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string id)
    {
        var result = await registerOrchestrator.ConfirmEmailAsync(id);

        const string frontendUrl = "http://localhost:4200";
        
        var redirectUrl = result.Success
            ? $"{frontendUrl}/email-confirmed?status=success"
            : $"{frontendUrl}/email-confirmed?status=error&message={Uri.EscapeDataString(result.Message)}";

        return Redirect(redirectUrl);
    }

}