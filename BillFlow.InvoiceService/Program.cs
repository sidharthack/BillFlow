using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Middleware;
using BillFlow.InvoiceService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;
using BillFlow.InvoiceService.Jobs;
using BillFlow.InvoiceService.Messaging;
using Hangfire;
using Hangfire.SqlServer;
// Add as the very first line before WebApplication.CreateBuilder
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);

// JWT settings — registered as singleton so middleware can resolve it
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

builder.Services.AddSingleton(jwtSettings);

// ASP.NET Core JWT authentication pipeline
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Only Admins can delete or cancel invoices
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Admins and Members can create/send invoices
    options.AddPolicy("CanWrite", policy =>
        policy.RequireRole("Admin", "Member"));

    // All authenticated users can read
    options.AddPolicy("CanRead", policy =>
        policy.RequireAuthenticatedUser());
});

// Tenant context — scoped per request
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp =>
    sp.GetRequiredService<TenantContext>());

// HttpClient for TenantService
builder.Services.AddHttpClient("TenantService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:TenantService"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient("CustomerService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(5);
});
// Add with business services
builder.Services.AddScoped<IInvoiceNumberService, InvoiceNumberService>();
builder.Services.AddScoped<ICustomerClient, CustomerClient>();
// Business services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// RabbitMQ publisher — Singleton because it holds a connection
builder.Services.AddSingleton<IEventPublisher>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();

    // Block here is acceptable for Singleton startup initialization
    return RabbitMqEventPublisher.CreateAsync(config, logger)
        .GetAwaiter()
        .GetResult();
});

// Hangfire — uses SQL Server for job storage
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("HangfireConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            SchemaName = "Hangfire"   // separate schema inside BillFlow_Invoices
        }
    )
);

// Hangfire server — processes background jobs
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;    // 2 concurrent job workers
    options.ServerName = "InvoiceService";
});

// Register the job class itself
builder.Services.AddScoped<OverdueInvoiceJob>();

var app = builder.Build();


app.UseHttpsRedirection();

// Order is critical:
// 1. Logging (sees everything)
// 2. Authentication (validates JWT signature)
// 3. TenantMiddleware (reads claims, populates ITenantContext)
// 4. Authorization (enforces [Authorize] attributes)
app.UseMiddleware<RequestLoggingMiddleware>();
// Hangfire dashboard — view job history at /hangfire
// In production: add auth to this endpoint
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => false,    // allow retrying jobs from UI
    Authorization = []              // open for now — lock down in Week 8
});

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// Schedule the overdue detection job — runs every day at 1:00 AM UTC
RecurringJob.AddOrUpdate<OverdueInvoiceJob>(
    recurringJobId: "overdue-invoice-detection",
    methodCall: job => job.ExecuteAsync(),
    cronExpression: "0 1 * * *",    // daily at 01:00 UTC
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    }
);
app.MapControllers();

app.Run();