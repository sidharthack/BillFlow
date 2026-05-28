using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace BillFlow.Contracts.Metrics;

public static class MetricsBootstrap
{
    public static void AddBillFlowMetrics(
    this IServiceCollection services,
    string serviceName)
    {
        services.AddSingleton(new MetricsContext(serviceName));

        // Only runtime metrics here (safe anytime)
        DotNetRuntimeStatsBuilder.Default().StartCollecting();
    }

    public class MetricsContext
    {
        public string ServiceName { get; }
        public MetricsContext(string serviceName) => ServiceName = serviceName;
    }

    public static void UseBillFlowMetrics(this WebApplication app)
    {
        app.UseMetricServer();

        app.UseHttpMetrics(options =>
        {
            options.AddCustomLabel("service", context =>
            {
                var ctx = context.RequestServices.GetService<MetricsContext>();
                return ctx?.ServiceName ?? "unknown";
            });
        });
    }
}