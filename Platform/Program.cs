using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Hubs;
using Platform.Repositories;
using Platform.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<GridDbContext>(options => options.UseInMemoryDatabase("GridDatabase"));

builder.Services.AddMemoryCache();
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
}).AddHubOptions<GridHub>(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Dependency injection
builder.Services.AddScoped<IGridRepository, GridRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Frontend origin
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});


var app = builder.Build();
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unhandled Exception: {ex.Message}");
        throw;
    }
});
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GridDbContext>();

    // Ensure the database is created
    dbContext.Database.EnsureCreated();

    if (!dbContext.GridCells.Any())
    {
        dbContext.GridCells.Add(new GridCell
        {
            Id = 1,
            GridData = new byte[2500],
            RevealedData = new byte[2500]
        });
        dbContext.SaveChanges();
    }
}
app.UseCors("CorsPolicy");
app.UseRouting();
app.MapHub<GridHub>("/gridhub");

app.Run();
