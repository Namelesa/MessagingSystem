using AutoMapper;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.WebApi.User.Contracts;

namespace MessagingSystem.Services.User.WebApi.User;

public class UserMap : Profile
{
    public UserMap()
    {
        CreateMap<EditUserContract, UserDto>();
        CreateMap<UserDto, Core.User.User>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.FirstName + src.LastName))
            .ForMember(dest => dest.NormalizedUserName,
                opt => opt.MapFrom(src => (src.FirstName + src.LastName).ToUpperInvariant()))
            .ForMember(dest => dest.Email,
                opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.NormalizedEmail,
                opt => opt.MapFrom(src => src.Email.ToUpperInvariant()))
            .ForMember(dest => dest.Login,
                opt => opt.MapFrom(src => src.Login))
            .ForMember(dest => dest.NickName,
                opt => opt.MapFrom(src => src.NickName));
    }
}