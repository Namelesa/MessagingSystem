using System.Text;
using FluentValidation;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.Encrypt;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using MessagingSystem.Services.User.Persistence;
using MessagingSystem.Services.User.Persistence.DbInitializer;
using MessagingSystem.Services.User.Persistence.User;
using MessagingSystem.Services.User.WebApi.Login;
using MessagingSystem.Services.User.WebApi.Register;
using MessagingSystem.Services.User.WebApi.User;
using MessagingSystem.Services.User.WebApi.User.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddScoped<IHasher, Hasher>();
builder.Services.AddScoped<IHasherPassword, HasherPassword>();
builder.Services.AddSingleton<IEncryptInfo, EncryptInfo>();

builder.Services.AddScoped<RegisterOrchestrator>();
builder.Services.AddScoped<LoginOrchestrator>();
builder.Services.AddScoped<UserOrchestrator>();

builder.Services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginValidator>();
builder.Services.AddScoped<IValidator<UserDto>, UserValidator>();

builder.Services.AddAutoMapper(config => config.AddProfile(new RegisterMap()));
builder.Services.AddAutoMapper(config => config.AddProfile(new LoginMap()));
builder.Services.AddAutoMapper(config => config.AddProfile(new UserMap()));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["JWTConfig:Issuer"],
        ValidAudience = builder.Configuration["JWTConfig:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTConfig:Key"] ?? string.Empty)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
        
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!context.Request.Cookies.ContainsKey("access_token")) 
                return Task.CompletedTask;
            var encryptedToken = context.Request.Cookies["access_token"];
            var decryptService = context.HttpContext.RequestServices.GetRequiredService<IEncryptInfo>();
            if (encryptedToken == null) 
                return Task.CompletedTask;
            var decryptedToken = decryptService.Decrypt(encryptedToken);
            context.Token = decryptedToken;

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Enter your JWT access token",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {jwtSecurityScheme, Array.Empty<string>()}
    });
});

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.Initialize();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();