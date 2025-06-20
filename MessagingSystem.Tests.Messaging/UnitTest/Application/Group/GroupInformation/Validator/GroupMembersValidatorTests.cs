using FluentValidation.TestHelper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Validator;

public class GroupMembersValidatorTests
{
    private readonly GroupMembersValidator _validator = new();
    
    [Fact]
    public void Should_Have_Error_When_Users_Is_Empty()
    {
        // Arrange
        var dto = new GroupMembersDto { Users = new List<string>() };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("Group must have between 1 and 40 users. 37");
    }

    [Fact]
    public void Should_Have_Error_When_Users_Count_Exceeds_Maximum()
    {
        // Arrange
        var users = Enumerable.Range(1, 38).Select(i => $"user{i}").ToList();
        var dto = new GroupMembersDto { Users = users };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("Group must have between 1 and 40 users. 37");
    }

    [Xunit.Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(37)]
    public void Should_Not_Have_Error_When_Users_Count_Is_Valid(int userCount)
    {
        // Arrange
        var users = Enumerable.Range(1, userCount).Select(i => $"user{i}").ToList();
        var dto = new GroupMembersDto { Users = users };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Should_Have_Error_When_Users_Count_Is_Zero()
    {
        // Arrange
        var dto = new GroupMembersDto { Users = new List<string>() };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Should_Pass_Validation_With_Minimum_Users_Count()
    {
        // Arrange
        var dto = new GroupMembersDto { Users = ["user1"] };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Should_Pass_Validation_With_Maximum_Users_Count()
    {
        // Arrange
        var users = Enumerable.Range(1, 37).Select(i => $"user{i}").ToList();
        var dto = new GroupMembersDto { Users = users };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Should_Validate_Successfully_With_Valid_Dto()
    {
        // Arrange
        var dto = new GroupMembersDto 
        { 
            Users = ["user1", "user2", "user3"]
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}