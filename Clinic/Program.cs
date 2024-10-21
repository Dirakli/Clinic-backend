
using Clinic.Data;
using Clinic.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Clinic.models;
using Clinic.Services;

namespace Clinic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
           // builder.Services.Configure<FolderOptions>(builder.Configuration.GetSection("FolderAddress"));
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddScoped<AUTH_PKG>();
            builder.Services.AddScoped<USER_PKG>();
            builder.Services.AddScoped<AuthHelper>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

            builder.Services.AddSingleton<IRedisService, RedisService>();



            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

                // Security configuration for Bearer tokens
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter JWT with Bearer prefix, e.g., 'Bearer {token}'",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http, // Change this to Http for Bearer tokens
                    Scheme = "Bearer",  // Specify the scheme as "Bearer"
                    BearerFormat = "JWT"  // Optional: specify that the format is JWT
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
            Array.Empty<string>() // Specify an empty array for scopes
        }
    });
            });


            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])
                     ),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }



            app.UseHttpsRedirection();
            app.UseAuthentication();
            // app.UseMiddleware<JwtMiddleware>();

            app.UseAuthorization();
            app.UseCors(builder =>
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader() 
                    .AllowAnyMethod() 
            );


            app.MapControllers();

            app.Run();
        }
    }
}
