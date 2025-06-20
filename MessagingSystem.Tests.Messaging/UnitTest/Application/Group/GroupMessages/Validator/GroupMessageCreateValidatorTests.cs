using FluentValidation.TestHelper;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Validators;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupMessages.Validator
{
    public class GroupMessageCreateValidatorTests
    {
        private readonly GroupMessageCreateValidator _validator = new();

        [Fact]
        public void GroupId_WhenEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var dto = new GroupMessageDto("user1@", "content", Guid.Empty);

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.GroupId)
                .WithErrorMessage("Group ID must be a valid GUID.");
        }

        [Fact]
        public void GroupId_WhenValidGuid_ShouldNotHaveValidationError()
        {
            // Arrange
            var dto = new GroupMessageDto("user1@", "content", Guid.NewGuid());

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.GroupId);
        }
    }
}