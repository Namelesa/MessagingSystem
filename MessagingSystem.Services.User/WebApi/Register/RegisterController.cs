using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.WebApi.Register.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.User.WebApi.Register;

[ApiController]
[Route("api/auth")]
public class RegisterController(RegisterOrchestrator registerOrchestrator, IMapper mapper) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([Required, FromBody] RegisterContract registerContract)
    {
        var registerDto = mapper.Map<RegisterDto>(registerContract);
        var result = await registerOrchestrator.RegisterUserAsync(registerDto);

        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    } 
    
    [HttpGet("confirm-email")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string id)
    {
        var result = await registerOrchestrator.ConfirmEmailAsync(id);
        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }
}