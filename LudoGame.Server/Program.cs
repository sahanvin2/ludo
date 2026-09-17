using LudoGame.Database;
using LudoGame.Server.Hubs;
using LudoGame.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite("Data Source=ludo_game.db"));

// SignalR
builder.Services.AddSignalR();

// Queue and Worker
builder.Services.AddSingleton<GameCommandQueue>();
builder.Services.AddSingleton<GameEngineWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GameEngineWorker>());

// Allow CORS so WinForms/clients can easily connect
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Auto-create database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.MapControllers();
app.MapHub<LudoHub>("/ludohub");

app.Run();
