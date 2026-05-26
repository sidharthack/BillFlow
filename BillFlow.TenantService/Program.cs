using BillFlow.Contracts.Health;
using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Metrics;
using BillFlow.TenantService.Data;
using BillFlow.TenantService.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

const string ServiceName = "TenantService";
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

    builder.Services.AddDbContext<TenantDbContext>(options =>
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
        .AddDbContextCheck<TenantDbContext>(
            name: "database",
            tags: ["ready"],
            customTestQuery: async (db, ct) =>
            {
                // Actually query the DB — not just check connection
                await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
                return true;
            });

    builder.Services.AddScoped<ITenantService, TenantService>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseBillFlowMetrics();

    app.UseMiddleware<ServiceCorrelationMiddleware>();

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