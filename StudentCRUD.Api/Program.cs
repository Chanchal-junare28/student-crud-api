using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StudentCRUD.Api.Data;
using StudentCRUD.Api.Repositories;
using StudentCRUD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------
// LOGGING (Critical for Azure debugging)
// -------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// -------------------------------------------------
// DATABASE CONFIGURATION (Azure + Local)
// -------------------------------------------------
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure()
        );
    }
});

// -------------------------------------------------
// CORS
// -------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// -------------------------------------------------
// MVC + Swagger
// -------------------------------------------------
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

// -------------------------------------------------
// Dependency Injection
// -------------------------------------------------
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

var app = builder.Build();

// -------------------------------------------------
// GLOBAL EXCEPTION HANDLER (Prevents IIS 500 Masking)
// -------------------------------------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Internal server error. Check logs.");
    });
});

// -------------------------------------------------
// APPLY MIGRATIONS (SAFE FOR AZURE)
// -------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Database migration failed:");
        Console.WriteLine(ex.Message);
        throw;
    }
}

// -------------------------------------------------
// MIDDLEWARE PIPELINE
// -------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();
