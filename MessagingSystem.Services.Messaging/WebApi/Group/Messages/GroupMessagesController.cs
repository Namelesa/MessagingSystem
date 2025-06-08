using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Messages;

[Authorize]
[ApiController]
[Route("api/messagingGroup")]
public class GroupMessagesController(
    IMapper mapper,
    IGroupMessagesOrchestrator groupMessagesOrchestrator
    ) : ControllerBase
{
    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessageAsync([Required] CreateGroupMessage message)
    {
        var messageForCreate = mapper.Map<GroupMessageDto>(message);

        var result = await groupMessagesOrchestrator.SendMessageAsync(messageForCreate);
        return Ok(result);
    }
    
    [HttpGet("find-message")]
    public async Task<IActionResult> FindMessageAsync([Required, FromQuery] MessageFilter filter)
    {
        var messages = await groupMessagesOrchestrator.FindMessagesAsync(filter);
        
        return Ok(messages);
    }
    
    [HttpGet("soft-delete-message")]
    public async Task<IActionResult> SoftDeleteMessageAsync([Required] Guid messageId)
    {
        var messages = await groupMessagesOrchestrator.SoftDeleteMessageAsync(messageId);
        
        return Ok(messages);
    }
    
    [HttpDelete("delete-message")]
    public async Task<IActionResult> DeleteMessageAsync([Required] Guid messageId)
    {
        var messages = await groupMessagesOrchestrator.DeleteMessageAsync(messageId);
        
        return Ok(messages);
    }
    
    [HttpGet("find-message-by-id")]
    public async Task<IActionResult> FindMessageByIdAsync([Required] Guid messageId)
    {
        var messages = await groupMessagesOrchestrator.FindMessageByIdAsync(messageId);
        
        return Ok(messages);
    }
    
    [HttpGet("load-chat-history")]
    public async Task<IActionResult> LoadChatMessagesAsync([Required] Guid groupId, int take)
    {
        var messages = await groupMessagesOrchestrator.LoadChatHistory(groupId, take);
        
        return Ok(messages);
    }
    
    [HttpPut("reply-message")]
    public async Task<IActionResult> ReplyMessageAsync([Required] Guid messageId, Guid replyId)
    {
        var messages = await groupMessagesOrchestrator.ReplyForMessageAsync(messageId, replyId);

        return Ok(messages);
    }
}