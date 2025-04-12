using FluentValidation;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Persistence;
using MessagingSystem.Services.User.Persistence.User;
using MessagingSystem.Services.User.WebApi.Register;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<RegisterOrchestrator>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

builder.Services.AddAutoMapper(config => config.AddProfile(new RegisterMap()));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();