using BillFlow.Contracts.Health;
using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Metrics;
using BillFlow.NotificationService.Consumers;
using BillFlow.NotificationService.Data;
using BillFlow.NotificationService.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Serilog;

const string ServiceName = "NotificationService";
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
    builder.Services.AddDbContext<NotificationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
        .AddDbContextCheck<NotificationDbContext>(
            name: "database",
            tags: ["ready"])
        .AddCheck("rabbitmq", () =>
        {
            // Check if RabbitMQ container is reachable
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                tcp.Connect("localhost", 5672);

                return HealthCheckResult.Healthy("RabbitMQ reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex);
            }
        }, tags: ["ready"]);

    // Services
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // ✓ Add — GetCompanyNameAsync calls /tenant/:slug for email branding
    builder.Services.AddHttpClient("TenantService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ServiceUrls:TenantService"]
            ?? "https://localhost:5001");

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

    // RabbitMQ consumer — runs for app lifetime
    builder.Services.AddHostedService<InvoiceEventConsumer>();

    var app = builder.Build();

    app.UseMiddleware<ServiceCorrelationMiddleware>();

    app.UseHttpsRedirection();
    app.UseBillFlowMetrics();

    app.UseAuthorization();

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