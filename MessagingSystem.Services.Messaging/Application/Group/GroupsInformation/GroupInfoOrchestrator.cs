using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;

public class GroupInfoOrchestrator(
    IGroupInfoRepository groupInfoRepository,
    IHasher hasher,
    IMapper mapper,
    IValidator<GroupDto> validator,
    IValidator<EditGroupDto> editValidator,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
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
        
        var checkResult = await userOrchestrator.CheckUsersAsync(groupInfo.Users);
        
        if (!checkResult.Success)
            return OperationResult<GroupDto>.Fail("Failed to check users existence");
        
        var foundUsers = checkResult.Data;
        if (foundUsers == null)
            return OperationResult<GroupDto>.Fail("No users found");
        
        var foundNickNames = foundUsers.Select(u => u.NickName).ToHashSet();
        var notFoundUsers = groupInfo.Users.Where(nick => !foundNickNames.Contains(nick)).ToList();
        
        var imageMap = foundUsers
            .ToDictionary(u => u.NickName, u => encryptionInfo.Encrypt(u.Image ?? u.NickName));

        if (notFoundUsers.Count > 0)
        {
            return OperationResult<GroupDto>.Fail(
                $"These users were not found: {string.Join(", ", notFoundUsers)}");
        }
        
        var adminHash = hasher.Hash(groupInfo.Admin);
        var groupNameHash = hasher.Hash(groupInfo.GroupName);
        
        var group = mapper.Map<GroupInfo>(groupInfo);
        group.SetMembersImages(imageMap);
        group.ApplyHashToMembers(hasher.Hash);
        group.SetHash(adminHash, groupNameHash);
        group.EncryptMembers(encryptionInfo.Encrypt);
        
        try
        {
            encryptionInfo.EncryptObjectStrings(group);
            await groupInfoRepository.CreateGroupAsync(group);
            return MapAndDecrypt(group);
        }
        catch (Exception e)
        {
            logger.LogError($"Can not create group reasons: {e}");
            return OperationResult<GroupDto>.Fail("Cannot create group. A group with this name may already exist.");
        }
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
        var group = await groupInfoRepository.FindGroupByIdAsync(id);
        if(group == null)
            return OperationResult<GroupDto>.Fail("Group not found");
        
        group.EncryptMembers(decryptionInfo.Decrypt);
        var groupWithMembers =  MapAndDecrypt(group);
        
        if(!groupWithMembers.Success || groupWithMembers.Data == null)
            return OperationResult<GroupDto>.Fail("Failed to decrypt group information");
        
        var membersWithImages = group.Members
            .Select(m => new UserInGroupDto(
                m.UserNickName,
                string.IsNullOrWhiteSpace(m.Image) ? null : decryptionInfo.Decrypt(m.Image)))
            .ToList();

        groupWithMembers.Data.SetMembers(membersWithImages);
        return groupWithMembers;
    }
    public async Task<OperationResult<GroupDto>> EditGroupInfoAsync(Guid id, EditGroupDto groupInfo)
    {
        var group = await groupInfoRepository.FindGroupByIdAsync(id);
        if (group == null)
            return OperationResult<GroupDto>.Fail("Group not found");

        var validation = await editValidator.ToOperationResultAsync(groupInfo);
        if (validation is { Success: false, Message: not null })
            return OperationResult<GroupDto>.Fail(validation.Message);

        decryptionInfo.DecryptObjectStrings(group);
        
        var updatedHash = hasher.Hash(groupInfo.GroupName);
        group.EditInfo(groupInfo.GroupName, 
            groupInfo.Image ?? groupInfo.GroupName, 
            groupInfo.Description, updatedHash);
        
        try
        {
            encryptionInfo.EncryptObjectStrings(group);
            await groupInfoRepository.EditGroupInfoAsync(group);
            return MapAndDecrypt(group);
        }
        catch (Exception e)
        {
            logger.LogError($"Can not update group reasons: {e}");
            return OperationResult<GroupDto>.Fail("Failed to update group: " + e.Message);
        }
    }
    public async Task<OperationResult<string>> EditGroupsAdminAsync(string adminHash, string newAdminNick)
    {
        var groupsAdmin = await groupInfoRepository.FindGroupByAdminHashAsync(adminHash);

        var newAdminHash = hasher.Hash(newAdminNick);
        
        foreach (var admin in groupsAdmin)
        {
            admin.SetAdminHash(newAdminHash);
            admin.EditAdminNick(encryptionInfo.Encrypt(newAdminNick));
            await groupInfoRepository.EditGroupInfoAsync(admin);
        }
        return OperationResult<string>.Ok("Update is ok");
    }
    public async Task<OperationResult<string>> DeleteGroupInfoAsync(Guid id, string adminNickName)
    {
        var adminHash = hasher.Hash(adminNickName);
        
        var group = await groupInfoRepository.FindGroupByIdAsync(id);
        if (group == null)
            return OperationResult<string>.Fail("Group not found");
        
        if (group.AdminHash != adminHash)
            return OperationResult<string>.Fail("You can't delete group");

        try
        {
            var deletedGroup = await groupInfoRepository.DeleteGroupAsync(group);
            return OperationResult<string>.Ok($"Group {decryptionInfo.Decrypt(deletedGroup.GroupName)} was deleted");
        }
        catch (Exception e)
        {
            logger.LogError($"Can not delete group reasons: {e}");
            return OperationResult<string>.Fail(e.ToString());
        }
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
            decryptionInfo.DecryptObjectStrings(g);
            g.EncryptMembers(decryptionInfo.Decrypt);
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
        var group = await groupInfoRepository.FindGroupByIdAsync(id);
        if (group == null)
            return OperationResult<GroupDto>.Fail("Group not found");

        if (group.AdminHash != adminHash)
            return OperationResult<GroupDto>.Fail("You can't add or delete members");

        group.EncryptMembers(decryptionInfo.Decrypt);

        try
        {
            var userNicks = users.ToList();
            if (userNicks.Count == 0)
                return OperationResult<GroupDto>.Fail("User list is empty");

            switch (modificationType)
            {
                case GroupMemberModificationType.Add:
                    group.AddUsers(userNicks);
                    break;
                case GroupMemberModificationType.Remove:
                    group.DeleteUsers(userNicks);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modificationType), modificationType, null);
            }

            group.EncryptMembers(encryptionInfo.Encrypt);
            group.ApplyHashToMembers(hasher.Hash);

            var editedGroup = await groupInfoRepository.EditGroupInfoAsync(group);
            return MapAndDecrypt(editedGroup);
        }
        catch (Exception e)
        {
            logger.LogError($"Can not modify user members in group. Reasons: {e}");
            return OperationResult<GroupDto>.Fail(e.ToString());
        }
    }
    private OperationResult<GroupDto> MapAndDecrypt(GroupInfo group)
    {
        var dto = mapper.Map<GroupDto>(group);
        decryptionInfo.DecryptObjectStrings(dto);
        return OperationResult<GroupDto>.Ok(dto);
    }
}