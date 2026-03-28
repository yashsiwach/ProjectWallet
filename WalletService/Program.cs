using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WalletService.Application.Interfaces;
using WalletService.Application.Services;
using WalletService.Infrastructure.Data;
using WalletService.Infrastructure.Email;
using WalletService.Infrastructure.Messaging;
using WalletService.Infrastructure.Repositories;
using WalletService.Middleware;

namespace WalletService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Infrastructure — Messaging
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Infrastructure — Email
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();

        // Infrastructure — Data
        builder.Services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Infrastructure — Repositories
        builder.Services.AddScoped<IWalletRepository, WalletRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Application — Services
        builder.Services.AddScoped<IWalletService, Application.Services.WalletService>();

        builder.Services.AddHttpClient("AuthService", client =>
            client.BaseAddress = new Uri(builder.Configuration["AuthService:BaseUrl"]!));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "WalletService", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Description = "Type: Bearer {your token}"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        // Global Exception Middleware — must be first
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAngular");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
