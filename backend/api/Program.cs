using api.Models;
using api.Services;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the MongoDBSettings configuration section
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));

// Register the UserServices as a singleton
builder.Services.AddSingleton<UserService>();

// Enable CORS to allow requests from the frontend(Call App from out side)
builder.Services.AddCors();


#region Jwt Configration
//Jwt Configration 
var jwtsecrete = builder.Configuration.GetSection("JwtSecret")["Secret"] ??
    throw new InvalidOperationException();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = true;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = "https://localhost:7017",
        ValidAudience = "https://localhost:7017",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsecrete)),
        ClockSkew = TimeSpan.Zero,
    };
}); 
#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();