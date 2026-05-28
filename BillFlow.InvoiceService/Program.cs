using BillFlow.Contracts.Health;
using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Metrics;
using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Jobs;
using BillFlow.InvoiceService.Messaging;
using BillFlow.InvoiceService.Middleware;
using BillFlow.InvoiceService.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
using QuestPDF.Infrastructure;
using Serilog;
using System.Text;

const string ServiceName = "InvoiceService";

// Add as the very first line before WebApplication.CreateBuilder
QuestPDF.Settings.License = LicenseType.Community;

// Use: "TenantService", "", "CustomerService",
//      "NotificationService" in respective services

SerilogBootstrap.Configure(ServiceName);

try
{
    var builder = WebApplication.CreateBuilder(args);

    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);
    builder.Services.AddBillFlowMetrics(ServiceName);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        // Liveness — is the process alive?
        .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"),
            tags: ["live"])

        // Readiness — can it serve traffic? (DB must be reachable)
        .AddDbContextCheck<AppDbContext>(
            name: "database",
            tags: ["ready"],
            customTestQuery: async (db, ct) =>
            {
                // Actually query the DB — not just check connection
                await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
                return true;
            });

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

    // ── TenantService client with resilience ─────────────────────────────────
    builder.Services.AddHttpClient("TenantService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ServiceUrls:TenantService"]
            ?? "https://localhost:5001");

        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddResilienceHandler("tenant-pipeline", pipeline =>
    {
        // Retry: 3 attempts with exponential backoff
        // 1st retry after 500ms, 2nd after 1s, 3rd after 2s
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(500),
            UseJitter = true,
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException } => PredicateResult.True(),
                { Result.StatusCode: >= System.Net.HttpStatusCode.InternalServerError }
                    => PredicateResult.True(),
                _ => PredicateResult.False()
            }
        });

        // Circuit breaker
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(15),
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException } => PredicateResult.True(),
                { Result.StatusCode: >= System.Net.HttpStatusCode.InternalServerError }
                    => PredicateResult.True(),
                _ => PredicateResult.False()
            }
        });

        // Timeout
        pipeline.AddTimeout(TimeSpan.FromSeconds(5));
    });

    // ── CustomerService client with resilience ────────────────────────────────
    builder.Services.AddHttpClient("CustomerService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ServiceUrls:CustomerService"]
            ?? "https://localhost:5003");

        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddResilienceHandler("customer-pipeline", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(500),
            UseJitter = true
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(15)
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(5));
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
                SchemaName = "Hangfire"
            }
        )
    );

    // Hangfire server — processes background jobs
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
        options.ServerName = "InvoiceService";
    });

    // Register the job class itself
    builder.Services.AddScoped<OverdueInvoiceJob>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseBillFlowMetrics();

    app.UseMiddleware<ServiceCorrelationMiddleware>();

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
        IsReadOnlyFunc = _ => false,
        Authorization = []
    });

    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();

    // Schedule the overdue detection job — runs every day at 1:00 AM UTC
    RecurringJob.AddOrUpdate<OverdueInvoiceJob>(
        recurringJobId: "overdue-invoice-detection",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "0 1 * * *",
        options: new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        }
    );

    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.Options.ResponseWriter,
        AllowCachingResponses = HealthCheckResponseWriter.Options.AllowCachingResponses,
        Predicate = check => check.Tags.Contains("live")
    });

    // Readiness — load balancer stops routing traffic if this fails
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.Options.ResponseWriter,
        AllowCachingResponses = HealthCheckResponseWriter.Options.AllowCachingResponses,
        Predicate = check => check.Tags.Contains("ready")
    });

    // Combined — what the gateway currently calls
    app.MapHealthChecks("/health",
        HealthCheckResponseWriter.Options);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}