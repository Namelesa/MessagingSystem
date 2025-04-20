using MessagingSystem.Services.Notification.Infrastructure.MailJet;

namespace MessagingSystem.Tests.Notification.UnitTests.Infrastructure
{
    public class MailJetSettingsTests
    {
        [Fact]
        public void MailJetSettings_ShouldInitializeCorrectly_WithGivenValues()
        {
            // Arrange
            var apiKey = "test-api-key";
            var secretKey = "test-secret-key";

            // Act
            var settings = new MailJetSettings
            {
                ApiKey = apiKey,
                SecretKey = secretKey
            };

            // Assert
            Assert.Equal(apiKey, settings.ApiKey);
            Assert.Equal(secretKey, settings.SecretKey);
        }

        [Fact]
        public void MailJetSettings_ShouldInitialize_WithNullValues_WhenNotProvided()
        {
            // Act
            var settings = new MailJetSettings();

            // Assert
            Assert.Null(settings.ApiKey);
            Assert.Null(settings.SecretKey);
        }

        [Fact]
        public void MailJetSettings_ShouldAllowPartialInitialization()
        {
            // Act
            var settings = new MailJetSettings { ApiKey = "test-api-key" };

            // Assert
            Assert.Equal("test-api-key", settings.ApiKey);
            Assert.Null(settings.SecretKey);
        }

        [Fact]
        public void MailJetSettings_ShouldAllowSecretKeyToBeNull()
        {
            // Act
            var settings = new MailJetSettings { SecretKey = null };

            // Assert
            Assert.Null(settings.SecretKey);
        }
    }
}