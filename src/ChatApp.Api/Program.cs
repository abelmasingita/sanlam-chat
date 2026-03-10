using ChatApp.Application;
using ChatApp.Api.Hubs;
using ChatApp.Api.Middleware;
using ChatApp.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Per-connection fixed-window rate limiter: max 5 messages per 5 seconds.
// Keyed by ConnectionId so each client is limited independently.
builder.Services.AddSingleton<PartitionedRateLimiter<string>>(
    PartitionedRateLimiter.Create<string, string>(connectionId =>
        RateLimitPartition.GetFixedWindowLimiter(connectionId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(5),
            QueueLimit = 0
        })));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Redis backplane synchronises SignalR across multiple API instances.
// It is optional - if no Redis connection string is configured the app runs as a single instance.
var signalR = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
    signalR.AddStackExchangeRedis(redisConnection);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
