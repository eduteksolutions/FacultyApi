using FacultyApi.Data;
using FacultyApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("DefaultConnection is missing");
}

// ================= DB =================
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= SERVICES =================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddScoped<IGeneralCoordinatesService, GeneralCoordinatesService>();
builder.Services.AddScoped<NotificationService>();
var app = builder.Build();

// ================= MIDDLEWARE =================
// 1. Exception handler first to catch any crash
app.UseDeveloperExceptionPage();

// 2. Protocols and Security CORS
app.UseWebSockets();
app.UseCors("AllowAll");

// 3. Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ================= API =================
app.MapControllers();

app.Run();