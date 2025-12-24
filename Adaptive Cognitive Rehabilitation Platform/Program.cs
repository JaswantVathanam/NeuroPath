using Adaptive_Cognitive_Rehabilitation_Platform.Components;
using AdaptiveCognitiveRehabilitationPlatform.Services;
using AdaptiveCognitiveRehabilitationPlatform.Services.GameAnalytics;
using AdaptiveCognitiveRehabilitationPlatform.Services.Authentication;
using NeuroPath.Services;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
// SQL/EF Core - Commented out for JSON-only mode
// using Microsoft.EntityFrameworkCore;
// using NeuroPath.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers for API endpoints
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Add HttpClient for Blazor components (using IHttpClientFactory pattern)
builder.Services.AddHttpClient();
// Note: Typed HttpClients for specific services are registered below

// Configure logging
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    if (builder.Environment.IsDevelopment())
    {
        config.SetMinimumLevel(LogLevel.Debug);
    }
});

// Add application services
builder.Services.AddScoped<AIDifficultyService>();
builder.Services.AddScoped<GameStatisticsService>();
builder.Services.AddScoped<PerformanceMetricsCalculator>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<AIAnalysisEngine>();
// SQL-dependent service - Commented out for JSON-only mode
// builder.Services.AddScoped<DataSeedingService>();

// Add JSON-based game stats service (for demo/presentation purposes)
builder.Services.AddSingleton<IJsonGameStatsService, JsonGameStatsService>();

// Add JSON-based activity stats service (for wellness activities)
builder.Services.AddSingleton<IJsonActivityStatsService, JsonActivityStatsService>();

// Add JSON-based user authentication service (for demo/presentation purposes)
builder.Services.AddSingleton<IJsonUserService, JsonUserService>();

// Add authentication services - Using JSON-based auth now
// builder.Services.AddScoped<IdentityService>(); // SQL-dependent - commented out
builder.Services.AddScoped<JwtTokenService>();

// SQL/EF Core DbContext - Commented out for JSON-only mode
// builder.Services.AddDbContext<NeuroPathDbContext>(options =>
// {
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
// });

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? "your-super-secret-key-here-change-in-production";
var issuer = jwtSettings["Issuer"] ?? "NeuroPathApp";
var audience = jwtSettings["Audience"] ?? "NeuroPathAppUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add AI Services with HttpClient
builder.Services.AddHttpClient<OllamaAIDifficultyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

builder.Services.AddHttpClient<GameAiAnalysisService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

builder.Services.AddHttpClient<AzureAIDifficultyService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:1234");
    client.Timeout = TimeSpan.FromSeconds(180);
});

builder.Services.AddHttpClient<ServerStatusService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHostedService<ServerStatusService>();

builder.Services.AddHttpClient<DynamicSortingContentService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

builder.Services.AddHttpClient<ActivityAIAnalysisService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:1234");
    client.Timeout = TimeSpan.FromSeconds(180);
});

var app = builder.Build();

// SQL/EF Core Migrations - Commented out for JSON-only mode
// All data is now stored in JSON files in the GameData folder
// using (var scope = app.Services.CreateScope())
// {
//     var dbContext = scope.ServiceProvider.GetRequiredService<NeuroPathDbContext>();
//     dbContext.Database.Migrate();
//     
//     // Seed data
//     var seedingService = scope.ServiceProvider.GetRequiredService<DataSeedingService>();
//     await seedingService.SeedDataAsync();
// }

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API routes
app.MapControllers();

app.Run();
