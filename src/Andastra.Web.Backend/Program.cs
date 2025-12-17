using Andastra.Web.Backend.Hubs;
using Andastra.Web.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:5001", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register game session manager
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowBlazor");
app.UseRouting();

app.MapHub<GameHub>("/gamehub");
app.MapControllers();

app.MapGet("/", () => "Andastra Game Backend API - Use /gamehub for SignalR connection");

app.Run();
