namespace BillFlow.Gateway.Endpoints;

public static class HealthEndpoint
{
    private static readonly (string Name, string Url)[] Services =
    [
        ("InvoiceService",   "https://localhost:5000/health"),
        ("TenantService",    "https://localhost:5001/health"),
        ("IdentityService",  "https://localhost:5002/health"),
        ("CustomerService",  "https://localhost:5003/health"),
        ("NotificationService", "https://localhost:5004/health")   // ← ADD

    ];

    public static void MapHealthEndpoints(this WebApplication app)
    {
        // GET /health — aggregated health of all services
        app.MapGet("/health", async (IHttpClientFactory factory) =>
        {
            var client = factory.CreateClient("HealthCheck");
            var results = new List<object>();
            var allHealthy = true;

            foreach (var (name, url) in Services)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    var healthy = response.IsSuccessStatusCode;
                    if (!healthy) allHealthy = false;

                    results.Add(new
                    {
                        service = name,
                        status = healthy ? "healthy" : "unhealthy",
                        statusCode = (int)response.StatusCode
                    });
                }
                catch (Exception ex)
                {
                    allHealthy = false;
                    results.Add(new
                    {
                        service = name,
                        status = "unreachable",
                        error = ex.Message
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
        })
        .WithTags("Health")
        .AllowAnonymous();

        // GET /health/gateway — just the gateway itself (for container health checks)
        app.MapGet("/health/gateway", () => Results.Ok(new
        {
            status = "healthy",
            service = "Gateway",
            timestamp = DateTime.UtcNow
        }))
        .AllowAnonymous();
    }
}