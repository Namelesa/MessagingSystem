using AutoMapper;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.WebApi.Login.Contracts;

namespace MessagingSystem.Services.User.WebApi.Login;

public class LoginMap : Profile
{
    public LoginMap()
    {
        CreateMap<LoginContract, LoginDto>();
    }
}