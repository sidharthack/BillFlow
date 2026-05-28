using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillFlow.Gateway.Endpoints;

public static class HealthEndpoint
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", async (HttpContext context) =>
        {
            // Resolve directly from the request's service provider
            // instead of injecting — guarantees we get the right instance
            var factory = context.RequestServices
                .GetRequiredService<IHttpClientFactory>();

            var config = context.RequestServices
                .GetRequiredService<IConfiguration>();

            var services = new[]
            {
                ("InvoiceService",      config["ServiceHealthUrls:Invoice"]
                    ?? "https://127.0.0.1:5000"),
                ("TenantService",       config["ServiceHealthUrls:Tenant"]
                    ?? "https://127.0.0.1:5001"),
                ("IdentityService",     config["ServiceHealthUrls:Identity"]
                    ?? "https://127.0.0.1:5002"),
                ("CustomerService",     config["ServiceHealthUrls:Customer"]
                    ?? "https://127.0.0.1:5003"),
                ("NotificationService", config["ServiceHealthUrls:Notification"]
                    ?? "https://127.0.0.1:5004"),
            };

            var client = factory.CreateClient("HealthCheck");
            var results = new List<object>();
            var allHealthy = true;

            foreach (var (name, baseUrl) in services)
            {
                try
                {
                    var response = await client.GetAsync($"{baseUrl}/health/live");
                    var healthy = response.IsSuccessStatusCode;
                    if (!healthy) allHealthy = false;

                    var body = await response.Content.ReadAsStringAsync();

                    results.Add(new
                    {
                        service = name,
                        status = healthy ? "healthy" : "unhealthy",
                        statusCode = (int)response.StatusCode,
                        detail = body
                    });
                }
                catch (Exception ex)
                {
                    allHealthy = false;
                    results.Add(new
                    {
                        service = name,
                        status = "unreachable",
                        statusCode = 0,
                        detail = ex.Message   // ← this will show the real error
                    });
                }
            }

            var payload = new
            {
                status = allHealthy ? "healthy" : "degraded",
                gateway = "healthy",
                timestamp = DateTime.UtcNow,
                services = results
            };

            return allHealthy
                ? Results.Ok(payload)
                : Results.Json(payload,
                    statusCode: StatusCodes.Status207MultiStatus);
        }).AllowAnonymous();

        app.MapGet("/health/gateway", () => Results.Ok(new
        {
            status = "healthy",
            service = "Gateway",
            timestamp = DateTime.UtcNow
        })).AllowAnonymous();
    }
}