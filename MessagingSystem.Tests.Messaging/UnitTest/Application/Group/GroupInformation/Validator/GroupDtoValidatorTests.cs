using FluentValidation.TestHelper;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Validator;

public class GroupDtoValidatorTests
{
    private readonly GroupDtoValidator _validator = new();

    #region GroupName Tests

    [Fact]
    public void GroupName_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("", null, "Valid Description", "ValidAdmin", ["user1", "user2", "user3"], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
              .WithErrorMessage("Group name cannot be empty.");
    }

    [Fact]
    public void GroupName_WhenNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("", null, "Valid Description", "ValidAdmin", ["user1", "user2", "user3"], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
              .WithErrorMessage("Group name cannot be empty.");
    }

    [Fact]
    public void GroupName_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("", null, "Valid Description", "ValidAdmin", ["user1", "user2", "user3"], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName)
              .WithErrorMessage("Group name cannot be empty.");
    }

    [Fact]
    public void GroupName_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", null, "Valid Description", "ValidAdmin", ["user1", "user2", "user3"], [1, 2, 3, 4]);
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GroupName);
    }

    #endregion

    #region Description Tests

    [Fact]
    public void Description_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Group", null, new string('a', 651), "ValidAdmin", ["user1", "user2", "user3"], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description must be between 1 and 650");
    }

    [Fact]
    public void Description_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("GroupName", "Image", string.Empty, "admin", [], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("The length of 'Description' must be at least 1 characters. You entered 0 characters.");
    }

    [Fact]
    public void Description_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user3"], 
            [1, 2, 3, 4]); 

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Admin Tests

    [Fact]
    public void Admin_WhenEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("GroupName", "image", "description", string.Empty, [], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Admin)
              .WithErrorMessage("Admin name cannot be empty.");
    }

    [Fact]
    public void Admin_WhenNull_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("GroupName", "image", "descriptions", null, [], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Admin)
              .WithErrorMessage("Admin name cannot be empty.");
    }

    [Fact]
    public void Admin_WhenTooShort_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("GroupName", null, "Descriptions", "abc", [], [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Admin)
            .WithErrorMessage("The length of 'Admin' must be at least 4 characters. You entered 3 characters.");
    }

    [Fact]
    public void Admin_WhenTooLong_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("ValidGroupName", "", "Descriptions", new string('a', 81), new List<string>(), [1,2,3,4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Admin)
              .WithErrorMessage("Admin name length must between 4 and 80");
    }

    [Fact]
    public void Admin_WhenValidLength_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user3"], 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Admin);
    }

    #endregion

    #region Users Tests
    
    [Fact]
    public void Users_WhenTooFew_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2"], 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("Group must have between 3 and 40 users.");
    }

    [Fact]
    public void Users_WhenTooMany_ShouldHaveValidationError()
    {
        // Arrange
        var users = Enumerable.Range(1, 41).Select(i => $"user{i}").ToList();
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            users, 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("Group must have between 3 and 40 users.");
    }

    [Fact]
    public void Users_WhenContainsDuplicates_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user1"], 
            [1, 2, 3, 4]); 
       
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("User list contains duplicate or empty entries.");
    }

    [Fact]
    public void Users_WhenContainsDuplicatesIgnoreCase_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "User2", "USER1"], 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("User list contains duplicate or empty entries.");
    }
    
    [Fact]
    public void Users_WhenContainsDuplicatesAfterTrim_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", " user2 ", "user1 "], 
            [1, 2, 3, 4]); 

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Users)
              .WithErrorMessage("User list contains duplicate or empty entries.");
    }

    [Fact]
    public void Users_WhenValidCount_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user3"], 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Users_WhenMaxValidCount_ShouldNotHaveValidationError()
    {
        // Arrange
        var users = Enumerable.Range(1, 40).Select(i => $"user{i}").ToList();
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            users, 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    [Fact]
    public void Users_WhenMinValidCount_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user3"], 
            [1, 2, 3, 4]); 
        
        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Users);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Validator_WhenAllFieldsValid_ShouldPassValidation()
    {
        // Arrange
        var dto = new GroupDto("Valid Group Name", 
            null, 
            "Valid Description", 
            "ValidAdmin",
            ["user1", "user2", "user3"], 
            [1, 2, 3, 4]); 

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_WhenAllFieldsInvalid_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var dto = new GroupDto("", 
            null, 
            new string('a', 651), 
            "abc",
            ["user1"], 
            [1, 2, 3, 4]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GroupName);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Admin);
        result.ShouldHaveValidationErrorFor(x => x.Users);
    }

    #endregion
}