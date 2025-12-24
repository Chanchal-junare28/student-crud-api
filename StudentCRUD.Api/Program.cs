using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StudentCRUD.Api.Data;
using StudentCRUD.Api.Repositories;
using StudentCRUD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------
// Database configuration (Environment-based)
// -----------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // LOCAL → SQLite
        options.UseSqlite(connectionString);
    }
    else
    {
        // PROD → Azure SQL
        options.UseSqlServer(connectionString);
    }
});

// ----------------------
// CORS (Angular dev)
// ----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ----------------------
// MVC + Swagger
// ----------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student CRUD API",
        Version = "v1"
    });
});

// ----------------------
// Dependency Injection
// ----------------------
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

var app = builder.Build();

// ----------------------
// Middleware pipeline
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student CRUD API V1");
        c.RoutePrefix = string.Empty; // Swagger at app root
    });
}

app.UseCors("AllowAngularDev");

// HTTPS redirection is safe for Azure App Service
app.UseHttpsRedirection();

app.MapControllers();

app.Run();