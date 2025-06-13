using AutoMapper;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.Users;

namespace MessagingSystem.Services.Messaging.WebApi.Messages.User;

public class UserMap : Profile
{
    public UserMap()
    {
        CreateMap<FoundedUser, UserImage>()
            .ConstructUsing(src => new UserImage(
                src.NickName,
                src.Image ?? src.NickName
            ));
    }
}