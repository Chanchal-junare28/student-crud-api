using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StudentCRUD.Api.Data;
using StudentCRUD.Api.Repositories;
using StudentCRUD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Database configuration
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found."
    );

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
// {
//     options.UseSqlServer(connectionString);
// });
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // LOCAL (still using Azure SQL intentionally)
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
            });
    }
    else
    {
        // PROD → Azure SQL
        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure();
            });
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
// Controllers + Swagger
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
// Logging (Azure-friendly)
// ----------------------
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ENVIRONMENT: {env}", app.Environment.EnvironmentName);
logger.LogInformation("Using Azure SQL connection");

// ----------------------
// Apply EF Core migrations
// ----------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed.");
        throw; // Fail fast – do NOT hide DB errors
    }
}

// ----------------------
// Middleware pipeline
// ----------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student CRUD API V1");
});

app.UseCors("AllowAngularDev");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
