using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.WebApi.Group.Messages.Contracts;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Messages;

public class GroupMessagesMapper : Profile
{
    public GroupMessagesMapper()
    {
        CreateMap<CreateGroupMessage, GroupMessageDto>();
        CreateMap<GroupMessageDto, GroupMessage>();
    }
}