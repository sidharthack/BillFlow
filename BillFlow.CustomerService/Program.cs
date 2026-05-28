using BillFlow.Contracts.Health;
using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Metrics;
using BillFlow.Contracts.Tenancy;
using BillFlow.CustomerService.Data;
using BillFlow.CustomerService.Middleware;
using BillFlow.CustomerService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Serilog;
using System.Text;

const string ServiceName = "CustomerService";
// Use: "TenantService", "IdentityService", "CustomerService",
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
    builder.Services.AddDbContext<CustomerDbContext>(options =>
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
        .AddDbContextCheck<CustomerDbContext>(
            name: "database",
            tags: ["ready"],
            customTestQuery: async (db, ct) =>
            {
                // Actually query the DB — not just check connection
                await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
                return true;
            });

    // JWT
    var jwtSettings = builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()!;

    builder.Services.AddSingleton(jwtSettings);

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

    builder.Services.AddAuthorization();

    // Tenant context
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp =>
        sp.GetRequiredService<TenantContext>());

    // HttpClient for TenantService
    builder.Services.AddHttpClient("TenantService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ServiceUrls:TenantService"] ?? "https://localhost:5001");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddResilienceHandler("tenant-pipeline", pipeline =>
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

    // Business services
    builder.Services.AddScoped<ICustomerService, CustomerService>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseBillFlowMetrics();

    app.UseMiddleware<ServiceCorrelationMiddleware>();

    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Liveness — Kubernetes restarts the pod if this fails
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