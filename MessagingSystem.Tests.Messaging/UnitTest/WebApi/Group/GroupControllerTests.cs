using AutoMapper;
using FluentAssertions;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ImageLoader;
using MessagingSystem.Services.Messaging.WebApi.Group.Info;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.Members;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group;

public class GroupControllerTests
{
    private readonly Mock<IGroupInfoOrchestrator> _orchestratorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IImageLoaderService> _imageLoaderServiceMock;
    private readonly GroupController _controller;

    public GroupControllerTests()
    {
        _orchestratorMock = new Mock<IGroupInfoOrchestrator>();
        _mapperMock = new Mock<IMapper>();
        _imageLoaderServiceMock = new Mock<IImageLoaderService>();
        _controller = new GroupController(_orchestratorMock.Object, _mapperMock.Object, _imageLoaderServiceMock.Object);
    }

    #region CreateGroupAsync Tests

    [Fact]
    public async Task CreateGroupAsync_ValidRequest_ReturnsOkResult()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.Length).Returns(1234);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        var createGroupRequest = new CreateGroup("Test Group", null, "Test Description", "admin123")
        {
            ImageFile = fileMock.Object
        };

        var groupDto = new GroupDto("Test Group", "mocked-image.jpg", "Test Description", "admin123", new List<string>(), new byte[8]);
        var successResult = OperationResult<GroupDto>.Ok(groupDto);

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("mocked-image.jpg");
    
        _mapperMock.Setup(m => m.Map<GroupDto>(createGroupRequest))
            .Returns(groupDto);

        _orchestratorMock.Setup(o => o.CreateGroupAsync(groupDto))
            .ReturnsAsync(successResult);

        // Mock HttpContext and ServiceProvider for SignalR Hub
        SetupSignalRMocks();

        // Act
        var result = await _controller.CreateGroupAsync(createGroupRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult);
    }

    [Fact]
    public async Task CreateGroupAsync_OrchestratorReturnsFailure_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.Length).Returns(1234);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        var createGroupRequest = new CreateGroup("Test Group", null, "Test Description", "admin123")
        {
            ImageFile = fileMock.Object
        };

        var groupDto = new GroupDto("Test Group", "mocked-image.jpg", "Test Description", "admin123", new List<string>(), new byte[8]);
        var failureResult = OperationResult<GroupDto>.Fail("Group creation failed"); 

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("mocked-image.jpg");

        _mapperMock.Setup(m => m.Map<GroupDto>(createGroupRequest))
            .Returns(groupDto);

        _orchestratorMock.Setup(o => o.CreateGroupAsync(groupDto))
            .ReturnsAsync(failureResult); 

        // Act
        var result = await _controller.CreateGroupAsync(createGroupRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>(); 
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be(failureResult);
    }

    [Fact]
    public async Task CreateGroupAsync_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var createGroupRequest = new CreateGroup("", "TestImage", "TestDescription", "admin123");
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateGroupAsync(createGroupRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var modelState = Assert.IsAssignableFrom<SerializableError>(badRequestResult.Value);

        modelState.Should().ContainKey("Name");
        ((string[])modelState["Name"]).Should().Contain("Name is required");
    }

    #endregion

    #region FindGroupAsync Tests

    [Fact]
    public async Task FindGroupAsync_ValidGroupName_ReturnsOkResult()
    {
        // Arrange
        var groupName = "TestGroup";
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto(groupName, "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _orchestratorMock.Setup(o => o.FindGroupByNameAsync(groupName))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.FindGroupAsync(groupName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult);
    }

    [Fact]
    public async Task FindGroupAsync_GroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupName = "NonExistentGroup";
        var failureResult = OperationResult<GroupDto>.Fail("Group not found");

        _orchestratorMock.Setup(o => o.FindGroupByNameAsync(groupName))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.FindGroupAsync(groupName);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.Value.Should().Be(failureResult);
    }

    [Xunit.Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindGroupAsync_EmptyOrWhitespaceGroupName_ReturnsBadRequest(string groupName)
    {
        // Act
        var result = await _controller.FindGroupAsync(groupName);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be("Group name cannot be empty");
    }

    #endregion

    #region FindGroupByIdAsync Tests

    [Fact]
    public async Task FindGroupByIdAsync_ValidId_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto("TestGroup", "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _orchestratorMock.Setup(o => o.FindGroupByIdAsync(groupId))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.FindGroupByIdAsync(groupId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult);
    }

    [Fact]
    public async Task FindGroupByIdAsync_GroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var failureResult = OperationResult<GroupDto>.Fail("Group not found");

        _orchestratorMock.Setup(o => o.FindGroupByIdAsync(groupId))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.FindGroupByIdAsync(groupId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.Value.Should().Be(failureResult);
    }

    #endregion

    #region GetGroupsForUserAsync Tests

    [Fact]
    public async Task GetGroupsForUserAsync_ValidUserNickName_ReturnsOkResult()
    {
        // Arrange
        var userNickName = "testUser";
        var successResult = OperationResult<List<GroupDto>>.Ok([
            new GroupDto("Group1", "Image1", "Description1", "admin123", new List<string>(), new byte[8]),
            new GroupDto("Group2", "Image2", "Description2", "admin456", new List<string>(), new byte[8])
        ]);

        _orchestratorMock.Setup(o => o.GetGroupsForUserAsync(userNickName))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.GetGroupsForUserAsync(userNickName);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult);
    }

    [Fact]
    public async Task GetGroupsForUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userNickName = "nonExistentUser";
        var failureResult = OperationResult<List<GroupDto>>.Fail("User not found");

        _orchestratorMock.Setup(o => o.GetGroupsForUserAsync(userNickName))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.GetGroupsForUserAsync(userNickName);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.Value.Should().Be(failureResult);
    }

    #endregion

    #region EditGroupAsync Tests

    [Fact]
    public async Task EditGroupAsync_ValidRequest_ReturnsOkResult()
{
    // Arrange
    var groupId = Guid.NewGuid();
    var editGroupRequest = new EditGroup("Updated Group", "Updated Image", "Updated Description");
    var editGroupDto = new EditGroupDto("Updated Group", "Updated Image", "Updated Description");
    var successResult = OperationResult<GroupDto>.Ok(new GroupDto("Updated Group", "Updated Image", "Updated Description", "admin123", new List<string>(), new byte[8]));

    _mapperMock.Setup(m => m.Map<EditGroupDto>(editGroupRequest))
               .Returns(editGroupDto);
    _orchestratorMock.Setup(o => o.EditGroupInfoAsync(groupId, editGroupDto))
                    .ReturnsAsync(successResult);
    
    var mockServiceProvider = new Mock<IServiceProvider>();
    var mockHubContext = new Mock<IHubContext<GroupChatHub>>();
    var mockClients = new Mock<IHubClients>();
    var mockClientProxy = new Mock<IClientProxy>();

    mockServiceProvider
        .Setup(sp => sp.GetService(typeof(IHubContext<GroupChatHub>)))
        .Returns(mockHubContext.Object);

    mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
    mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
    mockClientProxy
        .Setup(cp => cp.SendCoreAsync("EditGroupAsync", It.IsAny<object[]>(), default))
        .Returns(Task.CompletedTask);

    var mockHttpContext = new Mock<HttpContext>();
    mockHttpContext.Setup(ctx => ctx.RequestServices).Returns(mockServiceProvider.Object);

    _controller.ControllerContext = new ControllerContext()
    {
        HttpContext = mockHttpContext.Object
    };

    // Act
    var result = await _controller.EditGroupAsync(groupId, editGroupRequest);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult?.Value.Should().Be(successResult.Data);
    
    // Verify SignalR hub was called
    mockClientProxy.Verify(cp => cp.SendCoreAsync("EditGroupAsync", 
        It.Is<object[]>(args => args.Length == 1 && args[0] == successResult.Data), 
        default), Times.Once);
}

    [Fact]
    public async Task EditGroupAsync_EditFails_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var editGroupRequest = new EditGroup("Failed Group", "Failed Image", "Failed Description");
        var editGroupDto = new EditGroupDto("Failed Group", "Failed Image", "Failed Description");
        var failureResult = OperationResult<GroupDto>.Fail("Edit failed");

        _mapperMock.Setup(m => m.Map<EditGroupDto>(editGroupRequest))
                   .Returns(editGroupDto);
        _orchestratorMock.Setup(o => o.EditGroupInfoAsync(groupId, editGroupDto))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.EditGroupAsync(groupId, editGroupRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be(failureResult.Message);
    }

    #endregion

    #region AddMembersToGroupAsync Tests

    [Fact]
    public async Task AddMembersToGroupAsync_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var addMembersRequest = new AddMembers{ Users = { "user1", "user2" } };
        var membersDto = new GroupMembersDto{Users = {"user1", "user2"}};
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto("TestGroup", "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _mapperMock.Setup(m => m.Map<GroupMembersDto>(addMembersRequest))
                   .Returns(membersDto);
        _orchestratorMock.Setup(o => o.AddMembersToGroupAsync(groupId, membersDto, adminHash))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.AddMembersToGroupAsync(groupId, addMembersRequest, adminHash);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult.Data);
    }

    [Fact]
    public async Task AddMembersToGroupAsync_AddFails_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var addMembersRequest = new AddMembers { Users = { "user1", "user2" } };
        var membersDto = new GroupMembersDto { Users = { "user1", "user2" } };
        var failureResult = OperationResult<GroupDto>.Fail("Add members failed");

        _mapperMock.Setup(m => m.Map<GroupMembersDto>(addMembersRequest))
                   .Returns(membersDto);
        _orchestratorMock.Setup(o => o.AddMembersToGroupAsync(groupId, membersDto, adminHash))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.AddMembersToGroupAsync(groupId, addMembersRequest, adminHash);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be(failureResult.Message);
    }

    #endregion

    #region DeleteGroupAsync Tests

    [Fact]
    public async Task DeleteGroupAsync_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var successResult = OperationResult<string>.Ok("Group deleted successfully");

        _orchestratorMock.Setup(o => o.DeleteGroupInfoAsync(groupId, adminHash))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.DeleteGroupAsync(groupId, adminHash);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult.Data);
    }

    [Fact]
    public async Task DeleteGroupAsync_DeleteFails_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var failureResult = OperationResult<string>.Fail("Delete failed");

        _orchestratorMock.Setup(o => o.DeleteGroupInfoAsync(groupId, adminHash))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.DeleteGroupAsync(groupId, adminHash);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be(failureResult.Message);
    }

    #endregion

    #region RemoveMembersToGroupAsync Tests

    [Fact]
    public async Task RemoveMembersToGroupAsync_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var removeMembersRequest = new AddMembers { Users = { "user1", "user2" } };
        var membersDto = new GroupMembersDto { Users = { "user1", "user2" } };
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto("TestGroup", "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _mapperMock.Setup(m => m.Map<GroupMembersDto>(removeMembersRequest))
                   .Returns(membersDto);
        _orchestratorMock.Setup(o => o.DeleteMembersFromGroupAsync(groupId, membersDto, adminHash))
                        .ReturnsAsync(successResult);

        // Act
        var result = await _controller.RemoveMembersToGroupAsync(groupId, removeMembersRequest, adminHash);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(successResult.Data);
    }

    [Fact]
    public async Task RemoveMembersToGroupAsync_RemoveFails_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var adminHash = "admin123";
        var removeMembersRequest = new AddMembers { Users = { "user1", "user2" } };
        var membersDto = new GroupMembersDto { Users = { "user1", "user2" } };
        var failureResult = OperationResult<GroupDto>.Fail("Remove members failed");

        _mapperMock.Setup(m => m.Map<GroupMembersDto>(removeMembersRequest))
                   .Returns(membersDto);
        _orchestratorMock.Setup(o => o.DeleteMembersFromGroupAsync(groupId, membersDto, adminHash))
                        .ReturnsAsync(failureResult);

        // Act
        var result = await _controller.RemoveMembersToGroupAsync(groupId, removeMembersRequest, adminHash);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be(failureResult.Message);
    }

    #endregion

    #region Verify Method Calls

    [Fact]
    public async Task CreateGroupAsync_CallsMapperAndOrchestrator()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.Length).Returns(1234);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));

        var createGroupRequest = new CreateGroup("Test Group", null, "Test Description", "admin123")
        {
            ImageFile = fileMock.Object
        };

        var groupDto = new GroupDto("Test Group", "mocked-image.jpg", "Test Description", "admin123", new List<string>(), new byte[8]);
        var successResult = OperationResult<GroupDto>.Ok(groupDto);

        _imageLoaderServiceMock
            .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("mocked-image.jpg");
    
        _mapperMock.Setup(m => m.Map<GroupDto>(createGroupRequest))
            .Returns(groupDto);

        _orchestratorMock.Setup(o => o.CreateGroupAsync(groupDto))
            .ReturnsAsync(successResult);

        // Mock HttpContext and ServiceProvider for SignalR Hub
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockHubContext = new Mock<IHubContext<GroupChatHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IHubContext<GroupChatHub>)))
            .Returns(mockHubContext.Object);

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClientProxy
            .Setup(cp => cp.SendCoreAsync("CreateGroupAsync", It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(ctx => ctx.RequestServices).Returns(mockServiceProvider.Object);

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = mockHttpContext.Object
        };

        // Act
        await _controller.CreateGroupAsync(createGroupRequest);

        // Assert
        _imageLoaderServiceMock.Verify(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
        _mapperMock.Verify(m => m.Map<GroupDto>(createGroupRequest), Times.Once);
        _orchestratorMock.Verify(o => o.CreateGroupAsync(groupDto), Times.Once);
    
        // Verify SignalR hub was called
        mockClientProxy.Verify(cp => cp.SendCoreAsync("CreateGroupAsync", 
            It.Is<object[]>(args => args.Length == 1 && args[0] == successResult.Data), 
            default), Times.Once);
    }

    [Fact]
    public async Task FindGroupAsync_CallsOrchestratorWithCorrectParameter()
    {
        // Arrange
        var groupName = "TestGroup";
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto(groupName, "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _orchestratorMock.Setup(o => o.FindGroupByNameAsync(groupName))
                        .ReturnsAsync(successResult);

        // Act
        await _controller.FindGroupAsync(groupName);

        // Assert
        _orchestratorMock.Verify(o => o.FindGroupByNameAsync(groupName), Times.Once);
    }

    [Fact]
    public async Task FindGroupByIdAsync_CallsOrchestratorWithCorrectParameter()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var successResult = OperationResult<GroupDto>.Ok(new GroupDto("TestGroup", "TestImage", "TestDescription", "admin123", new List<string>(), new byte[8]));

        _orchestratorMock.Setup(o => o.FindGroupByIdAsync(groupId))
                        .ReturnsAsync(successResult);

        // Act
        await _controller.FindGroupByIdAsync(groupId);

        // Assert
        _orchestratorMock.Verify(o => o.FindGroupByIdAsync(groupId), Times.Once);
    }

    #endregion
    
    [Fact]
    public async Task CreateGroupAsync_WithValidImageFile_ShouldCallImageLoaderAndSetImageUrl()
{
    // Arrange
    var groupName = "Test Group";
    var description = "Test Description";
    var admin = "adminUser";

    var fileMock = new Mock<IFormFile>();
    fileMock.Setup(f => f.Length).Returns(100);
    fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

    var imageUrl = "https://cdn.example.com/test.jpg";

    _imageLoaderServiceMock
        .Setup(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()))
        .ReturnsAsync(imageUrl);

    var inputModel = new CreateGroup
    {
        GroupName = groupName,
        Description = description,
        Admin = admin,
        ImageFile = fileMock.Object
    };

    var mappedGroupDto = new GroupDto("Test Group", imageUrl, description, admin, new List<string>(), new byte[8]); 

    _mapperMock
        .Setup(m => m.Map<GroupDto>(inputModel))
        .Returns(mappedGroupDto);

    _orchestratorMock
        .Setup(o => o.CreateGroupAsync(mappedGroupDto))
        .ReturnsAsync(OperationResult<GroupDto>.Ok(mappedGroupDto));

    // Mock HttpContext and ServiceProvider for SignalR Hub
    SetupSignalRMocks();

    // Act
    var result = await _controller.CreateGroupAsync(inputModel);

    // Assert
    result.Should().BeOfType<OkObjectResult>();

    _imageLoaderServiceMock.Verify(s =>
        s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);

    inputModel.Image.Should().Be(imageUrl);
}
    
    [Fact]
    public async Task CreateGroupAsync_WithZeroLengthImageFile_ShouldSetEmptyImageUrl()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0); // Zero length file

        var createGroupRequest = new CreateGroup("Test Group", null, "Test Description", "admin123")
        {
            ImageFile = fileMock.Object
        };

        var groupDto = new GroupDto("Test Group", string.Empty, "Test Description", "admin123", new List<string>(), new byte[8]);
        var successResult = OperationResult<GroupDto>.Ok(groupDto);

        _mapperMock.Setup(m => m.Map<GroupDto>(createGroupRequest))
            .Returns(groupDto);
        _orchestratorMock.Setup(o => o.CreateGroupAsync(groupDto))
            .ReturnsAsync(successResult);

        SetupSignalRMocks();

        // Act
        var result = await _controller.CreateGroupAsync(createGroupRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        createGroupRequest.Image.Should().Be(string.Empty);
        _imageLoaderServiceMock.Verify(s => s.UploadOrReplaceAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }
    
    private void SetupSignalRMocks()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockHubContext = new Mock<IHubContext<GroupChatHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();

        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IHubContext<GroupChatHub>)))
            .Returns(mockHubContext.Object);

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClientProxy
            .Setup(cp => cp.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(ctx => ctx.RequestServices).Returns(mockServiceProvider.Object);

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = mockHttpContext.Object
        };
    }
}