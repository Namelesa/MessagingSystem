using AutoMapper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;

namespace MessagingSystem.Services.Messaging.WebApi.Group.Info;

public class GroupMap : Profile
{
    public GroupMap()
    {
        CreateMap<CreateGroup, GroupDto>()
            .ConstructUsing(src => new GroupDto(
                src.GroupName,
                src.Image,
                src.Description,
                src.Admin,
                src.Users,
                new byte[0]
            ));
        CreateMap<GroupDto, GroupInfo>()
            .ForMember(dest => dest.Members, opt => 
                opt.MapFrom(src =>
                src.Users
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(nick => new GroupMembers(nick))
                    .ToList()))
            .ForMember(dest => dest.Description, opt => 
                opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Id, opt => 
                opt.Ignore())
            .ForMember(dest => dest.AdminHash, opt => 
                opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore()); 
        CreateMap<GroupInfo, GroupDto>()
            .ForMember(dest => dest.Members, opt => opt.Ignore())
            .ConstructUsing(src => new GroupDto(
                src.GroupName,
                src.Image,
                src.Description,
                src.Admin,
                src.Members.Select(m => m.UserNickName).ToList(),
                src.RowVersion
            ));

        CreateMap<EditGroup, EditGroupDto>()
            .ForCtorParam("groupName", opt => opt.MapFrom(src => src.GroupName))
            .ForCtorParam("image", opt => opt.MapFrom(src => src.Image))
            .ForCtorParam("description", opt => opt.MapFrom(src => src.Description ?? string.Empty));

        CreateMap<AddMembers, GroupMembersDto>();
    }
}