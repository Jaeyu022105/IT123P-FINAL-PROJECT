using System.Threading.RateLimiting;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using FoodLens.Api.Data;
using FoodLens.Api.Middleware;
using FoodLens.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database setup
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = "Data Source=foodlens.db";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("mysql", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        options.UseSqlite(connectionString);
});

// USDA Client setup
var usdaBaseUrl = builder.Configuration["Usda:BaseUrl"] ?? "https://api.nal.usda.gov/fdc/v1/";
builder.Services.AddHttpClient<IUsdaClient, UsdaClient>(client =>
{
    client.BaseAddress = new Uri(usdaBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Register standard client for outgoing requests
builder.Services.AddHttpClient();

// DI Repository registration
builder.Services.AddScoped<IFoodLogRepository, FoodLogRepository>();
builder.Services.AddScoped<IDietGoalRepository, DietGoalRepository>();

// CoreWCF setup
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddScoped<DietExportService>();

// Controllers and XML formatting
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();   // accept xml header support

// Swagger OpenAPI
builder.Services.AddOpenApi();

// Rate limiting
// 60 requests limit per IP to guard USDA API quota
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

// CORS configuration
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Auto migrate db on start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware pipeline setup
app.UseMiddleware<ExceptionHandlingMiddleware>();   // Outermost middleware for catching exceptions
app.UseMiddleware<RequestLoggingMiddleware>();      // Logging requests
app.UseRateLimiter();                               // Rate limiter placement
app.UseCors();

// CoreWCF SOAP configuration
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<DietExportService>();
    serviceBuilder.AddServiceEndpoint<DietExportService, IDietExportService>(
        new CoreWCF.BasicHttpBinding(),
        "/soap/DietExportService.svc");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // openapi json endpoint
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();

app.Run();
