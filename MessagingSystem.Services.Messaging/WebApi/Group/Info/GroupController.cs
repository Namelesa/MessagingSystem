using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Infrastructure.ImageLoader;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        groupInfo.Image = GetImageUrl(groupInfo.ImageFile);
        
        var createGroup = mapper.Map<GroupDto>(groupInfo);
        
        var result = await groupInfoOrchestrator.CreateGroupAsync(createGroup);

        return result.Success 
            ? Ok(result) 
            : BadRequest(result);
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
        groupDto.Image = GetImageUrl(groupDto.ImageFile);
        
        var group = mapper.Map<EditGroupDto>(groupDto);
        
        var result = await groupInfoOrchestrator.EditGroupInfoAsync(id, group);

        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(result.Message);
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
    
    private string GetImageUrl(IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return string.Empty;

        var fileName = $"{Guid.NewGuid()}.jpg";
        return imageLoaderService.UploadOrReplaceAsync(imageFile.OpenReadStream(), fileName).Result;
    }
}