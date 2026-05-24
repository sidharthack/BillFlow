using BillFlow.Contracts.Health;
using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Tenancy;
using BillFlow.IdentityService.Data;
using BillFlow.IdentityService.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Serilog;

const string ServiceName = "IdentityService";
// Use: "TenantService", "", "CustomerService",
//      "NotificationService" in respective services

SerilogBootstrap.Configure(ServiceName);
try
{
    var builder = WebApplication.CreateBuilder(args);
    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);


    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Database
    builder.Services.AddDbContext<IdentityDbContext>(options =>
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
        .AddDbContextCheck<IdentityDbContext>(          // change per service:
            name: "database",                      // AppDbContext, TenantDbContext,
            tags: ["ready"],                       // IdentityDbContext, etc.
            customTestQuery: async (db, ct) =>
            {
                // Actually query the DB — not just check connection
                await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
                return true;
            });
    // JWT settings — binds appsettings.json JwtSettings section
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings"));

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

    // Services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    var app = builder.Build();


    app.UseHttpsRedirection();
    app.UseMiddleware<ServiceCorrelationMiddleware>();

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