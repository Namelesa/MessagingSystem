using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Persistence.Group;

public class GroupMemberRepositoryTestsDelete : IAsyncLifetime
{
    private IContainer _pgContainer;
    private string _connectionString;
    private GroupMemberRepository _repository;
    private IDbContextFactory<GroupAppDbContext> _factory;

    public async Task InitializeAsync()
    {
        _pgContainer = new ContainerBuilder()
            .WithImage("postgres:16")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "testdb")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _pgContainer.StartAsync();

        var port = _pgContainer.GetMappedPublicPort(5432);
        _connectionString =
            $"Host=localhost;Port={port};Database=testdb;Username=postgres;Password=postgres;Include Error Detail=true";

        var options = new DbContextOptionsBuilder<GroupAppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var initContext = new GroupAppDbContext(options);
        await initContext.Database.EnsureCreatedAsync();

        var groupId = Guid.NewGuid();
        var groupInfo = new GroupInfo("Group", "img", "desc", "admin") { Id = groupId };
        groupInfo.SetHash("admin_hash", "groupName_hash");

        var user1 = new GroupMembers("user1") { GroupId = groupId };
        user1.SetHash("hash1");

        var user2 = new GroupMembers("user2") { GroupId = groupId };
        user2.SetHash("hash2");

        var user3 = new GroupMembers("user3") { GroupId = groupId };
        user3.SetHash("keep_hash");

        initContext.GroupInfos.Add(groupInfo);
        initContext.GroupMembers.AddRange(user1, user2, user3);
        await initContext.SaveChangesAsync();
        
        var factoryMock = new Mock<IDbContextFactory<GroupAppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var opts = new DbContextOptionsBuilder<GroupAppDbContext>()
                    .UseNpgsql(_connectionString)
                    .Options;
                return new GroupAppDbContext(opts);
            });

        _factory = factoryMock.Object;
        _repository = new GroupMemberRepository(_factory);
    }

    [Fact]
    public async Task DeleteUsersByHashesAsync_WorksCorrectly_WithRealPostgres()
    {
        // Act
        var deletedCount = await _repository.DeleteUsersByHashesAsync(new[] { "hash1", "hash2" });

        // Assert
        Assert.Equal(2, deletedCount);
        
        await using var verifyContext = await _factory.CreateDbContextAsync();
        var remaining = await verifyContext.GroupMembers.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("keep_hash", remaining.First().UserNickNameHash);
    }

    public async Task DisposeAsync()
    {
        if (_pgContainer != null)
        {
            await _pgContainer.DisposeAsync();
        }
    }
}
