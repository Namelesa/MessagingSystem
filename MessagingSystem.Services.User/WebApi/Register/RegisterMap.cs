using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.WebApi.Register.Contracts;

namespace MessagingSystem.Services.User.WebApi.Register;

public class RegisterMap : Profile
{
    public RegisterMap()
    {
        CreateMap<RegisterContract, RegisterDto>();
        CreateMap<RegisterDto, Core.User.User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + src.LastName))
            .ForMember(dest => dest.NormalizedUserName, opt => opt.MapFrom(src => (src.FirstName + src.LastName).ToUpperInvariant()))
            .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToUpperInvariant()))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.SecurityStamp, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.HashLogin, opt => opt.Ignore())
            .ForMember(dest => dest.HashEmail, opt => opt.Ignore())
            .ForMember(dest => dest.HashNickName, opt => opt.Ignore());
    }
}