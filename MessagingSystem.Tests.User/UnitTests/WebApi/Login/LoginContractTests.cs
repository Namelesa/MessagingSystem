using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.Login.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Login
{
    public class LoginContractTests
    {
        [Theory]
        [InlineData("user_123", "pass123!", true)]
        [InlineData("us!r", "p@ss123", false)]
        [InlineData("user", "password", false)]
        [InlineData("user@123", "P@ssw0rd", true)]
        public void LoginContract_ShouldValidateCorrectly(string login, string password, bool isValid)
        {
            // Arrange
            var contract = new LoginContract(login, password, "Pass1@");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract, serviceProvider: null, items: null);

            // Act
            var isValidContract = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(isValid, isValidContract);
        }

        [Theory]
        [InlineData("user_123", "validPass1@", true)]
        [InlineData("!usr", "short", false)]
        [InlineData("user123", "noSpecialCharacter", false)]
        [InlineData("12345!", "valid@Pass1", true)]
        public void LoginContract_ValidatesPasswordAndLogin(string login, string password, bool expectedIsValid)
        {
            // Arrange
            var contract = new LoginContract(login, password, "Pass1@");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }

        [Theory]
        [InlineData("", "pass123@", false)] 
        [InlineData("validLogin", "", false)] 
        [InlineData("valid_login", "Valid@123", true)] 
        public void LoginContract_ShouldFailWithEmptyFields(string login, string password, bool expectedIsValid)
        {
            // Arrange
            var contract = new LoginContract(login, password, "Pass1@");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }

        [Fact]
        public void LoginContract_ShouldFailWhenLoginDoesNotMatchRegex()
        {
            // Arrange
            var invalidLogin = "us";
            var password = "ValidPass1!";
            var contract = new LoginContract(invalidLogin, password, "Pass1@");

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Login must be 5 to 20 characters long"));
        }

        [Fact]
        public void LoginContract_ShouldFailWhenPasswordDoesNotMatchRegex()
        {
            // Arrange
            var login = "validLogin";
            var invalidPassword = "short";
            var contract = new LoginContract(login, invalidPassword, "Pass1@");

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Password must be 5 to 15 characters long"));
        }
        
        [Fact]
        public void LoginSetter_ShouldBeValid_WhenValidLoginIsSet()
        {
            // Arrange
            var contract = new LoginContract("user123", "validPassword1!", "Pass1@")
            {
                Login = "newLogin_123"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void PasswordSetter_ShouldBeValid_WhenValidPasswordIsSet()
        {
            // Arrange
            var contract = new LoginContract("validLogin!", "validPassword123!", "Pass1@")
            {
                Password = "newValidPass1@"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid); 
        }

        [Fact]
        public void LoginSetter_ShouldFail_WhenInvalidLoginIsSet()
        {
            // Arrange
            var contract = new LoginContract("user123", "validPassword123!", "Pass1@")
            {
                Login = "us"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid); 
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Login must be 5 to 20 characters long"));
        }

        [Fact]
        public void PasswordSetter_ShouldFail_WhenInvalidPasswordIsSet()
        {
            // Arrange
            var contract = new LoginContract("validLogin", "validPassword123!", "Pass1@")
            {
                Password = "short"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid); 
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Password must be 5 to 15 characters long"));
        }

        [Fact]
        public void LoginSetter_ShouldFail_WhenLoginDoesNotMatchRegex()
        {
            // Arrange
            var contract = new LoginContract("user123", "validPassword123!", "Pass1@");
            
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Login must be 5 to 20 characters long"));
        }

        [Fact]
        public void PasswordSetter_ShouldFail_WhenPasswordDoesNotMatchRegex()
        {
            // Arrange
            var contract = new LoginContract("validLogin", "validPassword123!", "Pass1@")
            {
                Password = "12345"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid); 
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Password must be 5 to 15 characters long"));
        }
    }
}
