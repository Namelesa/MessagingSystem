using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.WebApi.Register.Contracts;

namespace MessagingSystem.Services.User.WebApi.Register;

public class RegisterMap : Profile
{
    public RegisterMap()
    {
        CreateMap<RegisterContract, RegisterDto>();
        CreateMap<RegisterDto, Core.User.User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FirstName + src.LastName))
            .ForMember(dest => dest.NormalizedUserName, opt => opt.MapFrom(src => (src.FirstName + src.LastName).ToUpperInvariant()))
            .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToUpperInvariant()))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.SecurityStamp, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()));

    }
}