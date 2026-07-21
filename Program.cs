using FacultyApi.Data;
using FacultyApi.@interface;
using FacultyApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ================= DB =================
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= SERVICES =================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();


builder.Services.AddHttpClient();
// ================= GPS SERVICES =================




// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            // SetIsOriginAllowed(_ => true) allows all origins dynamically,
            // which safely permits you to chain .AllowCredentials()
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR streaming
        });
});

builder.Services.AddScoped<IGeneralCoordinatesService, GeneralCoordinatesService>();


var app = builder.Build();

// ================= MIDDLEWARE =================
app.UseWebSockets();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();


// ================= SWAGGER =================
app.UseSwagger();
app.UseSwaggerUI();

// ================= API =================
app.MapControllers();

// ================= SIGNALR HUB =================

app.Run();

