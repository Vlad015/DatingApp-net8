﻿using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

                
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter 'Bearer YOUR_TOKEN' (without quotes) in the field below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                     {
                        new OpenApiSecurityScheme
                        {
                             Reference = new OpenApiReference
                             {
                                 Type = ReferenceType.SecurityScheme,
                                 Id = "Bearer"
                             }
                        },
                        Array.Empty<string>()
                     }
                 });
            });

            services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(config.GetConnectionString("SqlServerConnStr"));
            });
            services.AddCors(options =>
            {
                options.AddPolicy("DevCorsPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });

                options.AddPolicy("ProdCorsPolicy", policy =>
                {
                    policy.WithOrigins("https://app.datingapp.com") // your deployed frontend
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILikesRepository, LikesRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<UserActivity>();
            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"]!));

            services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
            services.AddSignalR();
            services.AddSingleton<PresenceTracker>();
            return services;
        }
    }
}
