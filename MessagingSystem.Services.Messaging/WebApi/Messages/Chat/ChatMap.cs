using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;

namespace MessagingSystem.Services.Messaging.WebApi.Messages.Chat;

public class ChatMap : Profile
{
    public ChatMap()
    {
        CreateMap<Core.Oto.OtoChats.Chat, ChatDto>()
            .ForMember(dest => dest.NickName, opt => opt.MapFrom(src => src.NickName))
            .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.Image));
    }
}