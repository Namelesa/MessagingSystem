using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Validator;

public class ValidatorExtensionsTests
{
    private readonly Mock<IValidator<TestModel>> _mockValidator = new();

    [Fact]
    public async Task ToOperationResultAsync_Should_Return_Success_When_Validation_Passes()
    {
        // Arrange
        var testModel = new TestModel { Name = "Valid Name" };
        var validationResult = new ValidationResult();

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(testModel, result.Data);
        Assert.Null(result.Message);
        _mockValidator.Verify(x => x.ValidateAsync(testModel, default), Times.Once);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Return_Failure_When_Validation_Fails_With_Single_Error()
    {
        // Arrange
        var testModel = new TestModel { Name = null };
        var validationFailure = new ValidationFailure("Name", "Name is required");
        var validationResult = new ValidationResult(new[] { validationFailure });

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(default, result.Data);
        Assert.Equal("Name is required", result.Message);
        _mockValidator.Verify(x => x.ValidateAsync(testModel, default), Times.Once);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Return_Failure_When_Validation_Fails_With_Multiple_Errors()
    {
        // Arrange
        var testModel = new TestModel { Name = null };
        var validationFailures = new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Email", "Email is invalid"),
            new ValidationFailure("Age", "Age must be positive")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(default, result.Data);
        Assert.Equal("Name is required; Email is invalid; Age must be positive", result.Message);
        _mockValidator.Verify(x => x.ValidateAsync(testModel, default), Times.Once);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Handle_Empty_Error_Messages()
    {
        // Arrange
        var testModel = new TestModel { Name = "Test" };
        var validationFailure = new ValidationFailure("Name", "");
        var validationResult = new ValidationResult(new[] { validationFailure });

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("", result.Message);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Work_With_Real_Validator()
    {
        // Arrange
        var validator = new TestModelValidator();
        var validModel = new TestModel { Name = "Valid Name", Email = "test@example.com", Age = 25 };

        // Act
        var result = await validator.ToOperationResultAsync(validModel);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(validModel, result.Data);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Work_With_Real_Validator_Invalid_Model()
    {
        // Arrange
        var validator = new TestModelValidator();
        var invalidModel = new TestModel { Name = "", Email = "invalid-email", Age = -1 };

        // Act
        var result = await validator.ToOperationResultAsync(invalidModel);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Name is required", result.Message);
        Assert.Contains("Email is invalid", result.Message);
        Assert.Contains("Age must be positive", result.Message);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Handle_Null_Instance()
    {
        // Arrange
        TestModel testModel = null;
        var validationFailure = new ValidationFailure("", "Instance cannot be null");
        var validationResult = new ValidationResult(new[] { validationFailure });

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Instance cannot be null", result.Message);
    }

    [Fact]
    public async Task ToOperationResultAsync_Should_Preserve_Original_Instance_In_Success_Result()
    {
        // Arrange
        var testModel = new TestModel { Name = "Test", Email = "test@example.com", Age = 30 };
        var validationResult = new ValidationResult();

        _mockValidator
            .Setup(x => x.ValidateAsync(testModel, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _mockValidator.Object.ToOperationResultAsync(testModel);

        // Assert
        Assert.True(result.Success);
        Assert.Same(testModel, result.Data);
    }
}

public class TestModel
{
    public string Name { get; init; }
    public string Email { get; init; }
    public int Age { get; init; }
}

public class TestModelValidator : AbstractValidator<TestModel>
{
    public TestModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email is invalid");

        RuleFor(x => x.Age)
            .GreaterThan(0)
            .WithMessage("Age must be positive");
    }
}