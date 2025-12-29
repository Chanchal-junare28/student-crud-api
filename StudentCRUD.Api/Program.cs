
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
// CORS
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

// ------------------------------------------------
// 🔹 Apply EF Core migrations SAFELY (Azure-ready)
// ------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Do NOT crash the app in Azure
        var logger = scope.ServiceProvider
                          .GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed during startup.");
    }
}

// ----------------------
// Middleware pipeline
// ----------------------

// Swagger enabled for both Dev & Prod (safe for APIs)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student CRUD API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAngularDev");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
