using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Validators;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Validators;
using MessagingSystem.Services.Messaging.Application.User;

namespace MessagingSystem.Services.Messaging.Application;

public static class AddApplication
{
    public static void AddApplicationLayer(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IMessageOrchestrator, MessageOrchestrator>();
        services.AddScoped<IChatOrchestrator, ChatOrchestrator>();
        services.AddScoped<IUserOrchestrator, UserOrchestrator>();
        services.AddScoped<IGroupInfoOrchestrator, GroupInfoOrchestrator>();
        services.AddScoped<IGroupMemberOrchestrator, GroupMemberOrchestrator>();
        services.AddScoped<IGroupMessagesOrchestrator, GroupMessagesOrchestrator>();
        services.AddScoped<IValidator<MessagesDto>, MessageCreateValidator>();
        services.AddScoped<IValidator<EditMessageDto>, MessageEditValidator>();
        services.AddScoped<IValidator<GroupDto>, GroupDtoValidator>();
        services.AddScoped<IValidator<EditGroupDto>, EditGroupDtoValidator>();
        services.AddScoped<IValidator<GroupMembersDto>, GroupMembersValidator>();
        services.AddScoped<IValidator<GroupMessageDto>, GroupMessageCreateValidator>();
        services.AddScoped<IGroupEncryption, GroupEncryptionDecorator>();
    }
}