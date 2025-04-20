using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Notification.WebApi;

[ApiController]
[Route("api/notification")]
public class NotificationController(INotificationOrchestrator notificationOrchestrator): ControllerBase
{
    [HttpPost("confirmRegister")]
    public async Task<IActionResult> SendConfirmEmailAsync([FromBody]UserDto userDto, [Required]string nickName)
    {
        var result = await notificationOrchestrator.SendConfirmEmailAsync(userDto, nickName);
        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }
    
    [HttpPost("deleteUser")]
    public async Task<IActionResult> SendDeleteUserEmailAsync([FromBody]UserDto userDto)
    {
        var result = await notificationOrchestrator.SendDeleteUserInfoEmailAsync(userDto);
        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }
    
    [HttpPost("editUser")]
    public async Task<IActionResult> SendEditUserEmailAsync([FromBody]UserDto userDto)
    {
        var result = await notificationOrchestrator.SendEditUserInfoEmailAsync(userDto);
        return result.Success
            ? Ok($"{result.Data}")
            : BadRequest($"{result.Message}");
    }
}