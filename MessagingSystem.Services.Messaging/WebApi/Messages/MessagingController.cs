using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Messaging.WebApi.Messages;

[Authorize]
[ApiController]
[Route("api/messaging")]
public class MessagingController(
    IUserOrchestrator userOrchestrator,
    IMessageOrchestrator messageOrchestrator
    ) : ControllerBase
{
    
    [HttpGet("find-user")]
    public async Task<IActionResult> CheckUserAsync([Required] string nickName)
    {
        var user = await userOrchestrator.CheckUserAsync(nickName);

        return Content(user, "text/plain");
    }
    
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var nick = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;

        if (string.IsNullOrEmpty(nick))
            return Unauthorized("Nickname not found in token");

        return Ok(new { nick });
    }

    [HttpGet("by-time")]
    public async Task<IActionResult> FindMessageAsync([Required, FromQuery] MessageFilter filter)
    {
        var messages = await messageOrchestrator.FindMessagesAsync(filter);
        
        return Ok(messages);
    }
}