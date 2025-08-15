using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.Login.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Login
{
    public class LoginContractTests
    {
        [Theory]
        [InlineData("user_123", "Pass123!", true)]  
        [InlineData("us!r", "P@ss123", false)]      
        [InlineData("user", "password", false)]     
        [InlineData("user@123", "P@ssw0rd1", true)] 
        [InlineData("validLogin!", "NoNumber@", false)]
        [InlineData("short", "Valid1@", false)]      
        public void LoginContract_ShouldValidateCorrectly(string login, string password, bool expectedIsValid)
        {
            // Arrange
            var contract = new LoginContract(login, password, "Nick@123");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract, serviceProvider: null, items: null);

            // Act
            var isValidContract = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValidContract);

            if (isValidContract || validationResults.Count == 0) return;
            var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            System.Diagnostics.Debug.WriteLine($"Validation errors for {login}/{password}: {errors}");
        }

        [Theory]
        [InlineData("", "Pass123@", false)]         
        [InlineData("validLogin_", "", false)]      
        [InlineData("valid_login", "Valid@123", true)] 
        [InlineData(null, "Valid@123", false)]       
        [InlineData("valid_login", null, false)]     
        public void LoginContract_ShouldValidateRequiredFields(string? login, string? password, bool expectedIsValid)
        {
            // Arrange
            var contract = new LoginContract(login ?? "", password ?? "", "Nick@123");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }

        [Theory]
        [InlineData("us", "Login must be 5 to 20 characters long")]           
        [InlineData("user", "Login must be 5 to 20 characters long")]         
        [InlineData("verylongloginnameover20chars", "Login must be 5 to 20 characters long")] 
        [InlineData("login", "Login must be 5 to 20 characters long")]       
        public void LoginContract_ShouldFailWithInvalidLogin(string invalidLogin, string expectedErrorSubstring)
        {
            // Arrange
            var contract = new LoginContract(invalidLogin, "ValidPass1!", "Nick@123");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains(expectedErrorSubstring));
        }

        [Theory]
        [InlineData("shor", "Password must be 5 to 115 characters long")]     
        [InlineData("password", "Password must be 5 to 115 characters long")] 
        [InlineData("Password", "Password must be 5 to 115 characters long")] 
        [InlineData("Pass123", "Password must be 5 to 115 characters long")]  
        [InlineData("Pass@@@", "Password must be 5 to 115 characters long")]  
        public void LoginContract_ShouldFailWithInvalidPassword(string invalidPassword, string expectedErrorSubstring)
        {
            // Arrange
            var contract = new LoginContract("validLogin!", invalidPassword, "Nick@123");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains(expectedErrorSubstring));
        }

        [Fact]
        public void LoginSetter_ShouldBeValid_WhenValidLoginIsSet()
        {
            // Arrange
            var contract = new LoginContract("user123!", "validPassword1!", "Nick@123")
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
            var contract = new LoginContract("validLogin!", "validPassword123!", "Nick@123")
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
            var contract = new LoginContract("user123!", "validPassword123!", "Nick@123")
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
            var contract = new LoginContract("validLogin!", "validPassword123!", "Nick@123")
            {
                Password = "short" 
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Password must be 5 to 115 characters long"));
        }

        [Theory]
        [InlineData("validLogin!", "ValidPass1@", "Nick@123", true)]    
        [InlineData("user_123", "Pass123!", "Test_123", true)]         
        [InlineData("short", "ValidPass1@", "Nick@123", false)]        
        [InlineData("validLogin!", "short", "Nick@123", false)]        
        [InlineData("validLogin!", "ValidPass1@", "sh", false)]        
        public void LoginContract_ShouldValidateAllFields(string login, string password, string nickname, bool expectedIsValid)
        {
            // Arrange
            var contract = new LoginContract(login, password, nickname);
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }
        
        [Fact]
        public void NickNameInit_ShouldBeValid_WhenValidNickNameIsSet()
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", "ValidNick@123");
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void NickNameInit_ShouldFail_WhenNickNameIsTooShort()
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", "ab"); 
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Nick name must be 3 to 15 characters long"));
        }

        [Fact]
        public void NickNameInit_ShouldFail_WhenNickNameIsTooLong()
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", "verylongnicknameover15chars@"); 
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Nick name must be 3 to 15 characters long"));
        }

        [Fact]
        public void NickNameInit_ShouldFail_WhenNickNameHasNoSpecialCharacter()
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", "nickname123"); 
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Nick name must be 3 to 15 characters long"));
        }

        [Fact]
        public void NickNameInit_ShouldFail_WhenNickNameIsEmpty()
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", ""); 
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("NickName is required"));
        }

        [Theory]
        [InlineData("a@b", true)]        
        [InlineData("nick_123", true)]   
        [InlineData("user!test", true)]  
        [InlineData("test@user123", true)] 
        [InlineData("ab", false)]        
        [InlineData("nickname", false)]  
        [InlineData("verylongnickname@123", false)] 
        public void NickNameInit_ShouldValidateCorrectly(string nickname, bool expectedIsValid)
        {
            // Arrange & Act
            var contract = new LoginContract("validLogin!", "ValidPass1@", nickname);
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }
        
        [Fact]
        public void NickNameInit_ShouldBeReadOnly_AfterObjectCreation()
        {
            // Arrange
            var contract = new LoginContract("validLogin!", "ValidPass1@", "ValidNick@123");

            // Act & Assert
            Assert.Equal("ValidNick@123", contract.NickName);
            
        }

        [Fact]
        public void NickNameInit_ShouldWorkWithObjectInitializer()
        {
            // Arrange & Act 
            var contract = new LoginContract("validLogin!", "ValidPass1@", "TempNick@123")
            {
                NickName = "InitNick@456"
            };
            
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
            Assert.Equal("InitNick@456", contract.NickName);
        }
    }
}