using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.WebApi.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.User.WebApi.User;

[Authorize]
[ApiController]
[Route("api/user")]
public class UsersController(UserOrchestrator userOrchestrator, IMapper mapper) : ControllerBase
{
    [HttpPut("edit")]
    public async Task<IActionResult> EditUserAsync([Required] string userId, [Required, FromBody] EditUserContract userContract)
    {
        var userDto = mapper.Map<UserDto>(userContract);
        var result = await userOrchestrator.EditUserInfoAsync(userDto, userId);

        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteUserAsync([Required] string id)
    {
        var result = await userOrchestrator.DeleteUserAsync(id);
        
        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }
}