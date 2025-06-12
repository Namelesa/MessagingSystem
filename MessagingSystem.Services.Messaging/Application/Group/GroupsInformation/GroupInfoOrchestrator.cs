using AutoMapper;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Application.User.Dto;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

public class GroupInfoOrchestrator(
    IGroupInfoRepository groupInfoRepository,
    IHasher hasher,
    IMapper mapper,
    IValidator<GroupDto> validator,
    IValidator<EditGroupDto> editValidator,
    IGroupEncryption groupEncryption,
    ILogger<GroupInfoOrchestrator> logger,
    IUserOrchestrator userOrchestrator
    ) : IGroupInfoOrchestrator
{
    public async Task<OperationResult<GroupDto>> CreateGroupAsync(GroupDto groupInfo)
    {
        groupInfo.AddAdminLikeUser(groupInfo.Admin.Trim());
        
        var validation = await validator.ToOperationResultAsync(groupInfo);
        if (!validation.Success) 
            return validation;
        
        var adminHash = hasher.Hash(groupInfo.Admin);
        var groupNameHash = hasher.Hash(groupInfo.GroupName);
        
        var group = mapper.Map<GroupInfo>(groupInfo);
        
        await AddUsersWithImagesToGroup(groupInfo.Users, group);
        
        group.ApplyHashToMembers(hasher.Hash);
        group.SetHash(adminHash, groupNameHash);
        groupEncryption.Encrypt(group);
        
        return await SafeExecuteAsync(async () =>
        {
            await groupInfoRepository.CreateGroupAsync(group);
            return MapAndDecrypt(group);
        }, "Cannot create group");

    }
    public async Task<OperationResult<GroupDto>> FindGroupByNameAsync(string groupName)
    {
        var groupNameHash = hasher.Hash(groupName);
        var group = await groupInfoRepository.FindGroupByNameHashAsync(groupNameHash);
        
        return group == null 
            ? OperationResult<GroupDto>.Fail("Group not found") 
            : MapAndDecrypt(group);
    }
    public async Task<OperationResult<GroupDto>> FindGroupByIdAsync(Guid id)
    {
        var group = await GetGroupByIdOrThrowAsync(id);
        group.EncryptMembers(groupEncryption.DecryptMembers);
        var groupWithMembers =  MapAndDecrypt(group);
        
        if(!groupWithMembers.Success || groupWithMembers.Data == null)
            return OperationResult<GroupDto>.Fail("Failed to decrypt group information");
        
        var membersWithImages = group.Members
            .Select(m => new UserInGroupDto(
                m.UserNickName,
                string.IsNullOrWhiteSpace(m.Image) ? null : groupEncryption.DecryptMembers(m.Image)))
            .ToList();

        groupWithMembers.Data.SetMembers(membersWithImages);
        return groupWithMembers;
    }
    public async Task<OperationResult<GroupDto>> EditGroupInfoAsync(Guid id, EditGroupDto groupInfo)
    {
        var validation = await editValidator.ToOperationResultAsync(groupInfo);
        if (validation is { Success: false, Message: not null })
            return OperationResult<GroupDto>.Fail(validation.Message);
        
        var group = await GetGroupByIdOrThrowAsync(id);
        groupEncryption.Decrypt(group);
        
        var updatedHash = hasher.Hash(groupInfo.GroupName);
        group.EditInfo(groupInfo.GroupName, 
            groupInfo.Image ?? groupInfo.GroupName, 
            groupInfo.Description, updatedHash);
        
        groupEncryption.Encrypt(group);
        
        return await SafeExecuteAsync(async () =>
        {
            await groupInfoRepository.EditGroupInfoAsync(group);
            return MapAndDecrypt(group);
        }, "Cannot update group");

    }
    public async Task<OperationResult<string>> EditGroupsAdminAsync(string adminHash, string newAdminNick)
    {
        var groupsAdmin = await groupInfoRepository.FindGroupByAdminHashAsync(adminHash);

        var newAdminHash = hasher.Hash(newAdminNick);
        
        foreach (var admin in groupsAdmin)
        {
            admin.SetAdminHash(newAdminHash);
            admin.EditAdminNick(groupEncryption.EncryptMembers(newAdminNick));
            await groupInfoRepository.EditGroupInfoAsync(admin);
        }
        return OperationResult<string>.Ok("Update is ok");
    }
    public async Task<OperationResult<string>> DeleteGroupInfoAsync(Guid id, string adminNickName)
    {
        var adminHash = hasher.Hash(adminNickName);
        
        var group = await GetGroupByIdOrThrowAsync(id);
        if (group.AdminHash != adminHash)
            return OperationResult<string>.Fail("You can't delete group");

        return await SafeExecuteAsync(async () =>
        {
            await groupInfoRepository.DeleteGroupAsync(group);
            return OperationResult<string>.Ok("Group deleted successfully");
        }, "Cannot delete group");
    }
    public Task<OperationResult<GroupDto>> AddMembersToGroupAsync(Guid id, GroupMembersDto dto, string adminHash) =>
        ModifyGroupMembersAsync(id, adminHash, dto.Users, GroupMemberModificationType.Add);
    public Task<OperationResult<GroupDto>> DeleteMembersFromGroupAsync(Guid id, GroupMembersDto dto, string adminHash) =>
        ModifyGroupMembersAsync(id, adminHash, dto.Users, GroupMemberModificationType.Remove);
    public async Task<OperationResult<List<GroupDto>>> GetGroupsForUserAsync(string userNick)
    {
        var userHash = hasher.Hash(userNick);
        var groups = await groupInfoRepository.GetGroupsByUserAsync(userHash);

        if (groups.Count == 0)
            return OperationResult<List<GroupDto>>.Fail("No groups found");

        var result = groups.Select(g =>
        {
            groupEncryption.Decrypt(g);
            g.EncryptMembers(groupEncryption.DecryptMembers);
            return mapper.Map<GroupDto>(g);
        }).ToList();
        
        return OperationResult<List<GroupDto>>.Ok(result);
    }
    private async Task<OperationResult<GroupDto>> ModifyGroupMembersAsync(
        Guid id,
        string adminHash,
        IEnumerable<string> users,
        GroupMemberModificationType modificationType)
    {
        var group = await GetGroupByIdOrThrowAsync(id);
        
        if (group.AdminHash != adminHash)
            return OperationResult<GroupDto>.Fail("You can't add or delete members");

        group.EncryptMembers(groupEncryption.DecryptMembers);
        return await SafeExecuteAsync(async () =>
        {
            var userNicks = users.ToList();
            if (userNicks.Count == 0)
                return OperationResult<GroupDto>.Fail("User list is empty");

            switch (modificationType)
            {
                case GroupMemberModificationType.Add:
                    await AddUsersWithImagesToGroup(userNicks, group);
                    group.AddUsers(userNicks);
                    break;
                case GroupMemberModificationType.Remove:
                    group.DeleteUsers(userNicks);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modificationType), modificationType, null);
            }

            group.EncryptMembers(groupEncryption.EncryptMembers);
            group.ApplyHashToMembers(hasher.Hash);

            var editedGroup = await groupInfoRepository.EditGroupInfoAsync(group);
            return MapAndDecrypt(editedGroup);
        }, "Can not modify user members in group");
    }
    private OperationResult<GroupDto> MapAndDecrypt(GroupInfo group)
    {
        var dto = mapper.Map<GroupDto>(group);
        groupEncryption.DecryptGeneric(dto);
        return OperationResult<GroupDto>.Ok(dto);
    }
    private async Task<OperationResult<List<FoundedUser>>> CheckUsersOrThrowAsync(List<string> userNickNames)
    {
        var checkResult = await userOrchestrator.CheckUsersAsync(userNickNames);

        if (!checkResult.Success)
            return OperationResult<List<FoundedUser>>.Fail(checkResult.Message ?? "Failed to check users");

        var foundUsers = checkResult.Data ?? throw new InvalidOperationException("No users found");

        var foundNickNames = foundUsers.Select(u => u.NickName).ToHashSet();
        var notFoundUsers = userNickNames.Where(nick => !foundNickNames.Contains(nick)).ToList();

        if (notFoundUsers.Count != 0)
            return OperationResult<List<FoundedUser>>.Fail(
                $"Users not found: {string.Join(", ", notFoundUsers)}");

        return OperationResult<List<FoundedUser>>.Ok(foundUsers);
    }
    private async Task AddUsersWithImagesToGroup(List<string> nickNames, GroupInfo group)
    {
        var foundUsers = await CheckUsersOrThrowAsync(nickNames);
        if (!foundUsers.Success || foundUsers.Data == null)
            throw new InvalidOperationException(foundUsers.Message);

        var imageMap = foundUsers.Data.ToDictionary(
            u => u.NickName,
            u => groupEncryption.EncryptMembers(u.Image ?? u.NickName));
        group.SetMembersImages(imageMap);
    }
    private async Task<OperationResult<T>> SafeExecuteAsync<T>(Func<Task<OperationResult<T>>> func, string errorMessage)
    {
        try
        {
            return await func();
        }
        catch (Exception e)
        {
            logger.LogError($"{errorMessage}: {e}");
            return OperationResult<T>.Fail(errorMessage);
        }
    }
    private async Task<GroupInfo> GetGroupByIdOrThrowAsync(Guid id)
    {
        var group = await groupInfoRepository.FindGroupByIdAsync(id);
        if (group == null)
            throw new InvalidOperationException("Group not found");
        return group;
    }
}