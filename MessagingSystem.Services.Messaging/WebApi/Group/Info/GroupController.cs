using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ImageLoader;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.Members;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Info;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupController(
    IGroupInfoOrchestrator groupInfoOrchestrator,
    IMapper mapper,
    IImageLoaderService imageLoaderService
) : ControllerBase
{
    [HttpPost("create-group")]
    public async Task<IActionResult> CreateGroupAsync([Required][FromForm] CreateGroup groupInfo)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        groupInfo.Image = await GetImageUrl(groupInfo.ImageFile);
        
        var createGroup = mapper.Map<GroupDto>(groupInfo);
        
        var result = await groupInfoOrchestrator.CreateGroupAsync(createGroup);

        if (!result.Success)
            return BadRequest(result);
        
        var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<GroupChatHub>>();
        await hubContext.Clients.All.SendAsync("CreateGroupAsync", result.Data);

        return Ok(result);
    }
    
    [HttpGet("find-group")]
    public async Task<IActionResult> FindGroupAsync([Required][FromQuery] string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return BadRequest("Group name cannot be empty");

        var result = await groupInfoOrchestrator.FindGroupByNameAsync(groupName);

        return result.Success 
            ? Ok(result) 
            : NotFound(result);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindGroupByIdAsync([Required] Guid id)
    {
        var result = await groupInfoOrchestrator.FindGroupByIdAsync(id);

        return result.Success 
            ? Ok(result) 
            : NotFound(result);
    }

    [HttpGet("find-chats")]
    public async Task<IActionResult> GetGroupsForUserAsync([Required] string userNickName)
    {
        var result = await groupInfoOrchestrator.GetGroupsForUserAsync(userNickName);
        return result.Success 
            ? Ok(result) 
            : NotFound(result);
    }

    [HttpPut("edit-group")]
    public async Task<IActionResult> EditGroupAsync([Required, FromQuery] Guid id, [Required, FromForm] EditGroup groupDto)
    {
        groupDto.Image = await GetImageUrl(groupDto.ImageFile);
        
        var group = mapper.Map<EditGroupDto>(groupDto);
        
        var result = await groupInfoOrchestrator.EditGroupInfoAsync(id, group);

        if(!result.Success)
            return BadRequest(result.Message);
        
        var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<GroupChatHub>>();
        await hubContext.Clients.All.SendAsync("EditGroupAsync", result.Data);
        
        return Ok(result.Data);
    }
    
    [HttpPut("add-members")]
    public async Task<IActionResult> AddMembersToGroupAsync([Required] Guid id, AddMembers addMembers, 
        [Required] string adminHash)
    {
        var members = mapper.Map<GroupMembersDto>(addMembers);
        var result = await groupInfoOrchestrator.AddMembersToGroupAsync(id, members, adminHash);

        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(result.Message);
    }
    
    [HttpDelete("delete-group")]
    public async Task<IActionResult> DeleteGroupAsync([Required] Guid id, [Required] string adminNickName)
    {
        var result = await groupInfoOrchestrator.DeleteGroupInfoAsync(id, adminNickName);

        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(result.Message);
    }
    
    [HttpDelete("delete-members")]
    public async Task<IActionResult> RemoveMembersToGroupAsync([Required] Guid id, AddMembers addMembers, 
        [Required] string adminHash)
    {
        var members = mapper.Map<GroupMembersDto>(addMembers);
        var result = await groupInfoOrchestrator.DeleteMembersFromGroupAsync(id, members, adminHash);

        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(result.Message);
    }
    
    private async Task<string> GetImageUrl(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return string.Empty;

        var fileName = $"{Guid.NewGuid()}.jpg";
        return await imageLoaderService.UploadOrReplaceAsync(imageFile.OpenReadStream(), fileName);
    }
}