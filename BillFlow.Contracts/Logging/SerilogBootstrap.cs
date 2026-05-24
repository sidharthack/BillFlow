using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace BillFlow.Contracts.Logging;

public static class SerilogBootstrap
{
    /// <summary>
    /// Configures Serilog for a BillFlow service.
    /// Call this before WebApplication.CreateBuilder().
    /// </summary>
    public static void Configure(
        string serviceName,
        string[]? args = null)
    {
        // Bootstrap logger for startup errors
        // (before appsettings is loaded)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        Log.Information("Starting {ServiceName}...", serviceName);
    }

    /// <summary>
    /// Wires Serilog into the WebApplicationBuilder.
    /// Call after WebApplication.CreateBuilder().
    /// </summary>
    public static void ConfigureBuilder(
        WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Host.UseSerilog((context, services, config) =>
        {
            var seqUrl = context.Configuration["Seq:Url"]
                ?? "http://localhost:5341";
            var seqApiKey = context.Configuration["Seq:ApiKey"];

            config
                // Minimum levels
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .MinimumLevel.Override("RabbitMQ", LogEventLevel.Warning)
                .MinimumLevel.Override("Yarp", LogEventLevel.Warning)

                // Enrichers — add context to every log line
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment",
                    context.HostingEnvironment.EnvironmentName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.With<CorrelationIdEnricher>()

                // Console sink — structured, colored output
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                    "{ServiceName} | {CorrelationId} | " +
                    "{Message:lj}{NewLine}{Exception}")

                // Seq sink — central log server
                .WriteTo.Seq(seqUrl, apiKey: seqApiKey);
        });
    }
}