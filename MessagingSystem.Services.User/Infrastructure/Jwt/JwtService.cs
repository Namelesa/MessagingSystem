using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MessagingSystem.Services.User.Infrastructure.Jwt;

public class JwtService(
    IConfiguration config, 
    ILogger<JwtService> logger, 
    IEncryptionInfo encryptInfo,
    IHttpContextAccessor httpContextAccessor) : IJwtService
{
    private readonly IConfiguration _config = config 
                                              ?? throw new ArgumentNullException(nameof(config));
    private readonly ILogger<JwtService> _logger = logger 
                                                   ?? throw new ArgumentNullException(nameof(logger));
    private readonly IEncryptionInfo _encryptInfo = encryptInfo 
                                                 ?? throw new ArgumentNullException(nameof(encryptInfo));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor 
                                                                 ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly PasswordHasher<LoginDto> _passwordHasher = new();

    private async Task<string?> AuthenticateAsync(LoginDto? user, string passwordRequest)
    {
        if (!ValidateUserCredentials(user, passwordRequest))
        {
            _logger.LogWarning("Authentication failed: invalid credentials for user {UserLogin}", user?.Login);
            return null;
        }

        try
        {
            var token = _encryptInfo.Encrypt(GenerateJwtToken(user));
            return await Task.FromResult(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating JWT token for user {UserLogin}", user?.Login);
            return null;
        }
    }
    
    public async Task<bool> AuthenticateAndSetCookieAsync(LoginDto? user, string passwordRequest)
    {
        var token = await AuthenticateAsync(user, passwordRequest);
        if (token == null)
            return false;
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_config.GetValue<int>("JWTConfig:TokenValidityMinutes"))
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token", token, cookieOptions);
        return true;
    }

    private bool ValidateUserCredentials(LoginDto? user, string passwordRequest)
    {
        if (user?.Password == null) 
            return false;

        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, passwordRequest, user.Password);
        return passwordVerificationResult == PasswordVerificationResult.Success;
    }

    private string GenerateJwtToken(LoginDto? user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuer = _config["JWTConfig:Issuer"];
        var audience = _config["JWTConfig:Audience"];
        var key = _config["JWTConfig:Key"];
        var tokenMin = _config.GetValue<int>("JWTConfig:TokenValidityMinutes");

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT key is not configured properly.");

        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(tokenMin);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(issuedAt).ToString(), ClaimValueTypes.Integer64),
            new (ClaimTypes.Name, user.Login)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}