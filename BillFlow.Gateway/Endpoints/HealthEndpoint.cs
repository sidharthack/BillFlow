namespace BillFlow.Gateway.Endpoints;

public static class HealthEndpoint
{
    private static readonly (string Name, string BaseUrl)[] Services =
    [
        ("InvoiceService",   "https://localhost:5000/health"),
        ("TenantService",    "https://localhost:5001/health"),
        ("IdentityService",  "https://localhost:5002/health"),
        ("CustomerService",  "https://localhost:5003/health"),
        ("NotificationService", "https://localhost:5004/health")   // ← ADD

    ];
    public static void MapHealthEndpoints(this WebApplication app)
    {
        // GET /health — aggregated status of all services
        app.MapGet("/health", async (IHttpClientFactory factory) =>
        {
            var client = factory.CreateClient("HealthCheck");
            var results = new List<object>();
            var allHealthy = true;

            foreach (var (name, baseUrl) in Services)
            {
                try
                {
                    var response = await client.GetAsync($"{baseUrl}/health");
                    var healthy = response.IsSuccessStatusCode;
                    if (!healthy) allHealthy = false;

                    // Parse the JSON body for detailed check info
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
                        detail = ex.Message
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

        // GET /health/gateway — just the gateway (for container probes)
        app.MapGet("/health/gateway", () => Results.Ok(new
        {
            status = "healthy",
            service = "Gateway",
            timestamp = DateTime.UtcNow
        })).AllowAnonymous();

        // GET /health/{service}/live — proxy liveness probe
        app.MapGet("/health/{service}/live", async (
            string service,
            IHttpClientFactory factory) =>
        {
            var match = Services.FirstOrDefault(s =>
                s.Name.Equals(service + "service",
                    StringComparison.OrdinalIgnoreCase) ||
                s.Name.Equals(service,
                    StringComparison.OrdinalIgnoreCase));

            if (match == default)
                return Results.NotFound(new { error = $"Service '{service}' not found" });

            try
            {
                var client = factory.CreateClient("HealthCheck");
                var response = await client.GetAsync($"{match.BaseUrl}/health/live");
                var body = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? Results.Ok(body)
                    : Results.Json(body,
                        statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).AllowAnonymous();

        // GET /health/{service}/ready — proxy readiness probe
        app.MapGet("/health/{service}/ready", async (
            string service,
            IHttpClientFactory factory) =>
        {
            var match = Services.FirstOrDefault(s =>
                s.Name.Equals(service + "service",
                    StringComparison.OrdinalIgnoreCase) ||
                s.Name.Equals(service,
                    StringComparison.OrdinalIgnoreCase));

            if (match == default)
                return Results.NotFound(new { error = $"Service '{service}' not found" });

            try
            {
                var client = factory.CreateClient("HealthCheck");
                var response = await client.GetAsync($"{match.BaseUrl}/health/ready");
                var body = await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? Results.Ok(body)
                    : Results.Json(body,
                        statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).AllowAnonymous();
    }
}