using Microsoft.EntityFrameworkCore;
using ProfisysTask.Configuration;
using ProfisysTask.Data;
using ProfisysTask.Middleware;
using ProfisysTask.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'Default' is not configured. Set it in appsettings or environment variables.");

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.Configure<CsvImportOptions>(builder.Configuration.GetSection(CsvImportOptions.SectionName));
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddTransient<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors();
app.MapControllers();

app.Run();
