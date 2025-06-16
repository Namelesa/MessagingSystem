using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.User.WebApi.User.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.User;

public class UserContractTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties_Correctly()
    {
        // Arrange
        const string firstName = "John";
        const string lastName = "Doe";
        const string login = "@login123";
        const string email = "john.doe@example.com";
        const string nickName = "nick_@name";

        // Act
        var contract = new EditUserContract(firstName, lastName, login, email, nickName);

        // Assert
        Assert.Equal(firstName, contract.FirstName);
        Assert.Equal(lastName, contract.LastName);
        Assert.Equal(login, contract.Login);
        Assert.Equal(email, contract.Email);
        Assert.Equal(nickName, contract.NickName);
    }

    [Fact]
    public void Validation_ShouldPass_WithValidFields()
    {
        // Arrange
        var contract = new EditUserContract("John", "Smith", "@login1", "john@example.com", "nick@name");

        // Act
        var results = new List<ValidationResult>();
        var context = new ValidationContext(contract);
        var isValid = Validator.TryValidateObject(contract, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Jo")] 
    [InlineData("ThisNameIsWayTooLongToBeValid")] 
    public void Validation_ShouldFail_WhenFirstNameIsInvalid(string invalidFirstName)
    {
        var contract = new EditUserContract(invalidFirstName, "Smith", "@login1", "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("First name length"));
    }

    [Theory]
    [InlineData("Li")] 
    [InlineData("ThisIsAVeryVeryLongLastNameIndeed")]
    public void Validation_ShouldFail_WhenLastNameIsInvalid(string invalidLastName)
    {
        var contract = new EditUserContract("John", invalidLastName, "@login1", "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Last name length"));
    }

    [Theory]
    [InlineData("login")] 
    [InlineData("lo")]    
    [InlineData("thisloginiswaytoolongandinvalid!")] 
    public void Validation_ShouldFail_WhenLoginIsInvalid(string invalidLogin)
    {
        var contract = new EditUserContract("John", "Smith", invalidLogin, "john@example.com", "nick@name");

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Login must be"));
    }
    
    [Theory]
    [InlineData("ni")] 
    [InlineData("nicknameistoolong@")] 
    [InlineData("nickname")] 
    public void Validation_ShouldFail_WhenNickNameIsInvalid(string invalidNick)
    {
        var contract = new EditUserContract("John", "Smith", "@login1", "john@example.com", invalidNick);

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(contract, new ValidationContext(contract), results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Nick name must be"));
    }
    [Fact]
        public void EditUserContract_EmptyConstructor_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var contract = new EditUserContract();

            // Assert
            Assert.Equal(string.Empty, contract.FirstName);
            Assert.Equal(string.Empty, contract.LastName);
            Assert.Equal(string.Empty, contract.Login);
            Assert.Equal(string.Empty, contract.Email);
            Assert.Equal(string.Empty, contract.NickName);
            Assert.Equal(string.Empty, contract.Image);
            Assert.Null(contract.ImageFile);
        }

        [Fact]
        public void EditUserContract_EmptyConstructor_WithObjectInitializer_ShouldValidateCorrectly()
        {
            // Arrange & Act
            var contract = new EditUserContract()
            {
                FirstName = "John",
                LastName = "Doe",
                Login = "john_doe123",
                Email = "john.doe@example.com",
                NickName = "johnny@123"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void EditUserContract_EmptyConstructor_ShouldFailValidationWithDefaultValues()
        {
            // Arrange
            var contract = new EditUserContract();
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("First name is required"));
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Last name is required"));
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("Login is required"));
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("required")); // Email required
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains("NickName is required"));
        }

        [Theory]
        [InlineData("John", "Doe", "john_doe@", "john@example.com", "nick@123", true)]
        [InlineData("Jo", "Doe", "john_doe@", "john@example.com", "nick@123", false)] 
        [InlineData("John", "Do", "john_doe@", "john@example.com", "nick@123", false)] 
        [InlineData("John", "Doe", "john", "john@example.com", "nick@123", false)] 
        [InlineData("John", "Doe", "john_doe@", "john@example.com", "nick", false)] 
        public void EditUserContract_ParameterizedConstructor_ShouldValidateCorrectly(
            string firstName, string lastName, string login, string email, string nickName, bool expectedIsValid)
        {
            // Arrange
            var contract = new EditUserContract(firstName, lastName, login, email, nickName);
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.Equal(expectedIsValid, isValid);
        }

        [Fact]
        public void EditUserContract_ParameterizedConstructor_WithOptionalParameters_ShouldWork()
        {
            // Arrange & Act
            var contract = new EditUserContract(
                "John", 
                "Doe", 
                "john_doe@", 
                "john@example.com", 
                "nick@123",
                "profile.jpg"
            );

            // Assert
            Assert.Equal("John", contract.FirstName);
            Assert.Equal("Doe", contract.LastName);
            Assert.Equal("john_doe@", contract.Login);
            Assert.Equal("john@example.com", contract.Email);
            Assert.Equal("nick@123", contract.NickName);
            Assert.Equal("profile.jpg", contract.Image);
            Assert.Null(contract.ImageFile);
        }

        [Theory]
        [InlineData("A", "First name length must be between 3 and 25 characters")] // Too short
        [InlineData("Verylongfirstnameexceeding25characters", "First name length must be between 3 and 25 characters")] // Too long
        public void EditUserContract_FirstName_ShouldFailWithInvalidLength(string firstName, string expectedError)
        {
            // Arrange
            var contract = new EditUserContract()
            {
                FirstName = firstName,
                LastName = "ValidLast",
                Login = "valid_login@",
                Email = "test@example.com",
                NickName = "nick@123"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains(expectedError));
        }

        [Theory]
        [InlineData("B", "Last name length must be between 3 and 25 characters")] 
        [InlineData("Verylonglastnameexceeding25characters", "Last name length must be between 3 and 25 characters")] 
        public void EditUserContract_LastName_ShouldFailWithInvalidLength(string lastName, string expectedError)
        {
            // Arrange
            var contract = new EditUserContract()
            {
                FirstName = "ValidFirst",
                LastName = lastName,
                Login = "valid_login@",
                Email = "test@example.com",
                NickName = "nick@123"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(contract);

            // Act
            var isValid = Validator.TryValidateObject(contract, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v => v.ErrorMessage != null && v.ErrorMessage.Contains(expectedError));
        }

        [Fact]
        public void EditUserContract_ImageSetter_ShouldWork()
        {
            // Arrange
            var contract = new EditUserContract("John", "Doe", "john_doe@", "john@example.com", "nick@123")
                {
                    // Act
                    Image = "new-image.jpg"
                };

            // Assert
            Assert.Equal("new-image.jpg", contract.Image);
        }
}
