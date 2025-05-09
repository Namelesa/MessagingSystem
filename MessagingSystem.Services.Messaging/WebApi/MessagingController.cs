using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Messaging.Application.User;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Messaging.WebApi;

[ApiController]
[Route("api/messaging")]
public class MessagingController(IUserOrchestrator userOrchestrator) : ControllerBase
{
    [HttpGet("check-user")]
    public async Task<IActionResult> CheckUserAsync([Required] string nickName)
    {
        var user = await userOrchestrator.CheckUserAsync(nickName);

        return Ok(user);
    }
}