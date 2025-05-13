using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Messages.Dto;
using MessagingSystem.Services.Messaging.Core.Messages;
using MessagingSystem.Services.Messaging.WebApi.Messages.Contracts;

namespace MessagingSystem.Services.Messaging.WebApi.Messages;

public class MessageMap : Profile
{
    public MessageMap()
    {
        CreateMap<CreateMessage, MessagesDto>();
        CreateMap<MessagesDto, Message>();
    }
    
}