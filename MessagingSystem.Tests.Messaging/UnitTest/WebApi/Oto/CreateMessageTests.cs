using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Messaging.WebApi.Messages.Contracts;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Oto;

public class CreateMessageTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange
        const string sender = "user_1";
        const string recipient = "admin@";
        const string content = "Hello World!";

        // Act
        var message = new CreateMessage(sender, recipient, content);

        // Assert
        Assert.Equal(sender, message.Sender);
        Assert.Equal(recipient, message.Recipient);
        Assert.Equal(content, message.Content);
    }

    [Fact]
    public void Constructor_WithNullParameters_SetsNullValues()
    {
        // Act
        var message = new CreateMessage(null, null, null);

        // Assert
        Assert.Null(message.Sender);
        Assert.Null(message.Recipient);
        Assert.Null(message.Content);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_SetsEmptyValues()
    {
        // Act
        var message = new CreateMessage("", "", "");

        // Assert
        Assert.Equal("", message.Sender);
        Assert.Equal("", message.Recipient);
        Assert.Equal("", message.Content);
    }

    #endregion

    #region Init Properties Tests

    [Fact]
    public void Properties_AreInitOnly_CannotBeModifiedAfterConstruction()
    {
        // Arrange
        var message = new CreateMessage("user_1", "admin@", "Hello");
        
        Assert.Equal("user_1", message.Sender);
        Assert.Equal("admin@", message.Recipient);
        Assert.Equal("Hello", message.Content);
    }

    [Fact]
    public void ObjectInitializer_WithInitProperties_WorksCorrectly()
    {
        // Act
        var message = new CreateMessage("temp", "temp", "temp")
        {
            Sender = "user_1",
            Recipient = "admin@",
            Content = "Hello from initializer"
        };

        // Assert
        Assert.Equal("user_1", message.Sender);
        Assert.Equal("admin@", message.Recipient);
        Assert.Equal("Hello from initializer", message.Content);
    }

    #endregion

    #region Sender Validation Tests
    
    [Fact]
    public void Sender_WhenNull_ReturnsRequiredErrorMessage()
    {
        // Arrange
        var message = new CreateMessage(null, "valid_user", "Hello");
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(message, context, results, true);

        // Assert
        var senderError = results.FirstOrDefault(r => r.MemberNames.Contains("Sender"));
        Assert.NotNull(senderError);
        Assert.Equal("Sender is required", senderError.ErrorMessage);
    }

    [Fact]
    public void Sender_WhenInvalidFormat_ReturnsRegexErrorMessage()
    {
        // Arrange
        var message = new CreateMessage("invaliduser", "valid_user", "Hello");
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(message, context, results, true);

        // Assert
        var senderError = results.FirstOrDefault(r => r.MemberNames.Contains("Sender"));
        Assert.NotNull(senderError);
        Assert.Contains("Sender name must be 3 to 15 characters long", senderError.ErrorMessage);
    }

    #endregion

    #region Recipient Validation Tests
    
    [Fact]
    public void Recipient_WhenNull_ReturnsRequiredErrorMessage()
    {
        // Arrange
        var message = new CreateMessage("valid_user", null, "Hello");
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(message, context, results, true);

        // Assert
        var recipientError = results.FirstOrDefault(r => r.MemberNames.Contains("Recipient"));
        Assert.NotNull(recipientError);
        Assert.Equal("Recipient is required", recipientError.ErrorMessage);
    }

    #endregion

    #region Content Validation Tests
    
    [Fact]
    public void Content_ExceedsMaxLength_IsInvalid()
    {
        // Arrange
        var content = new string('A', 1901); 
        var message = new CreateMessage("user_1", "admin@", content);
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(message, context, results, true);

        // Assert
        var contentError = results.FirstOrDefault(r => r.MemberNames.Contains("Content"));
        Assert.NotNull(contentError);
        Assert.Contains("Content length must be between 1 and 1900 characters", contentError.ErrorMessage);
    }

    [Fact]
    public void Content_WhenNull_ReturnsRequiredErrorMessage()
    {
        // Arrange
        var message = new CreateMessage("user_1", "admin@", null);
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(message, context, results, true);

        // Assert
        var contentError = results.FirstOrDefault(r => r.MemberNames.Contains("Content"));
        Assert.NotNull(contentError);
        Assert.Equal("Content is required", contentError.ErrorMessage);
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public void Validation_WithAllInvalidFields_ReturnsMultipleErrors()
    {
        // Arrange
        var message = new CreateMessage(null, null, null);
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(message, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.MemberNames.Contains("Sender"));
        Assert.Contains(results, r => r.MemberNames.Contains("Recipient"));
        Assert.Contains(results, r => r.MemberNames.Contains("Content"));
    }

    [Fact]
    public void Validation_WithValidData_PassesAllValidation()
    {
        // Arrange
        var message = new CreateMessage("sender_1", "recipient@", "Valid message content");
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(message, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Properties_WithUnicodeCharacters_AreHandledCorrectly()
    {
        // Arrange
        const string sender = "user_User"; 
        const string recipient = "admin@";
        const string content = "Hello world";

        // Act
        var message = new CreateMessage(sender, recipient, content);

        // Assert
        Assert.Equal(sender, message.Sender);
        Assert.Equal(recipient, message.Recipient);
        Assert.Equal(content, message.Content);
    }

    [Fact]
    public void Properties_ImmutabilityAfterConstruction_IsEnforced()
    {
        // Arrange
        var originalSender = "user_1";
        var originalRecipient = "admin@";
        var originalContent = "Hello";
        
        // Act
        var message = new CreateMessage(originalSender, originalRecipient, originalContent);
        
        // Assert
        Assert.Equal(originalSender, message.Sender);
        Assert.Equal(originalRecipient, message.Recipient);
        Assert.Equal(originalContent, message.Content);
        
        Assert.Same(originalSender, message.Sender);
        Assert.Same(originalRecipient, message.Recipient);
        Assert.Same(originalContent, message.Content);
    }

    #endregion
}