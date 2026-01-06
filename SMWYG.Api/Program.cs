using Microsoft.EntityFrameworkCore;
using SMWYG;
using SMWYG.Api.Hubs;
using SMWYG.Api.Background;

var builder = WebApplication.CreateBuilder(args);

// Load additional configuration from the desktop app's appsettings (contains connection string)
var desktopConfigPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SMWYG", "appsettings.json");
if (File.Exists(desktopConfigPath))
{
    builder.Configuration.AddJsonFile(desktopConfigPath, optional: true, reloadOnChange: true);
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure DbContext - use Npgsql (Postgres). Falls back to InMemory if no connection string provided.
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(conn))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(conn));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("SMWYG"));
}

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add SignalR for future real-time messaging
builder.Services.AddSignalR();

// Static files for uploads
builder.Services.AddDirectoryBrowser();

// Register cleanup background service
builder.Services.AddHostedService<CleanupJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();

// serve uploads
var uploadsRoot = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles();
app.UseDirectoryBrowser();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
