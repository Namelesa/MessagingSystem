using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Messaging.WebApi.Group;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupController(
    IGroupInfoOrchestrator groupInfoOrchestrator
    ) : ControllerBase
{
    [HttpPost("create-group")]
    public async Task<IActionResult> CreateGroupAsync([Required] GroupInfo groupInfo)
    {
        var result = await groupInfoOrchestrator.CreateGroupAsync(groupInfo);

        return Ok(result);
    }
}