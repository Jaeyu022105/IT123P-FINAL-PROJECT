using System.Threading.RateLimiting;
using FoodLens.Api.Data;
using FoodLens.Api.Middleware;
using FoodLens.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=foodlens.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("mysql", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        options.UseSqlite(connectionString);
});

// ── USDA HttpClient ──────────────────────────────────────────────────────────
var usdaBaseUrl = builder.Configuration["Usda:BaseUrl"] ?? "https://api.nal.usda.gov/fdc/v1/";
builder.Services.AddHttpClient<IUsdaClient, UsdaClient>(client =>
{
    client.BaseAddress = new Uri(usdaBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// ── Repositories & Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IFoodLogRepository, FoodLogRepository>();
builder.Services.AddScoped<IDietGoalRepository, DietGoalRepository>();

// ── MVC + XML formatter ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();   // enables Accept: application/xml on all endpoints

// ── OpenAPI / Swagger (built-in .NET 10) ────────────────────────────────────
builder.Services.AddOpenApi();

// ── Rate Limiter (built-in ASP.NET Core 7+) ─────────────────────────────────
// 60 requests per minute per IP — protects the free USDA quota from abuse.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
    options.RejectionStatusCode = 429;
});

// ── CORS (allow MAUI dev host) ───────────────────────────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Auto-migrate on startup (dev convenience) ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware Pipeline (order matters — see 01-architecture.md) ─────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();   // 1. Outermost — catches everything
app.UseMiddleware<RequestLoggingMiddleware>();      // 2. Log every request
app.UseRateLimiter();                               // 3. Rate-limit before heavy work
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // serves at /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
