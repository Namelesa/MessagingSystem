using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.WebApi.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.User.WebApi.User;

[Authorize]
[ApiController]
[Route("api/user")]
public class UsersController(
    IUserOrchestrator userOrchestrator, 
    IMapper mapper,
    IImageLoaderService imageLoaderService) : ControllerBase
{
    [HttpPut("edit")]
    public async Task<IActionResult> EditUserAsync([Required] string userId, [Required, FromForm] EditUserContract userContract)
    {
        if (userContract.ImageFile is { Length: > 0 })
        {
            await using var stream = userContract.ImageFile.OpenReadStream();
            var fileName = $"{Guid.NewGuid()}.jpg";
            var url = await imageLoaderService.UploadOrReplaceAsync(stream, fileName);
            userContract.Image = url;
        }
        
        var userDto = mapper.Map<UserDto>(userContract);
        var result = await userOrchestrator.EditUserInfoAsync(userDto, userId);

        if (!result.Success) 
            return BadRequest($"{result.Message}");
        
        Redirect("api/auth/login");
        return Ok($"{result.Data}");
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteUserAsync([Required] string id)
    {
        var result = await userOrchestrator.DeleteUserAsync(id);
        
        if (!result.Success) 
            return BadRequest($"{result.Message}");
        
        Redirect("api/auth/register");
        return Ok($"{result.Data}");
    }
}