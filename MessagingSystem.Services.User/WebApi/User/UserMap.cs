using AutoMapper;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.WebApi.User.Contracts;

namespace MessagingSystem.Services.User.WebApi.User
{
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
                    opt => opt.MapFrom(src => src.NickName))
                .ForMember(dest => dest.HashLogin, opt => opt.Ignore())   
                .ForMember(dest => dest.HashEmail, opt => opt.Ignore())
                .ForMember(dest => dest.HashNickName, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false)) 
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) 
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore()) 
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore()) 
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore()) 
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore()) 
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore()) 
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore()); 
        }
    }
}
