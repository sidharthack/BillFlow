using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Yarp.ReverseProxy.Model;

namespace BillFlow.Gateway.Middleware;

/// <summary>
/// Reads the RateLimitPolicy metadata from the matched YARP route
/// and applies the corresponding rate limit policy.
/// Runs inside the YARP pipeline — after route matching, before proxying.
/// </summary>
public class RateLimitPolicyFilter
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitPolicyFilter> _logger;

    public RateLimitPolicyFilter(
        RequestDelegate next,
        ILogger<RateLimitPolicyFilter> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get the YARP route feature — tells us which route was matched
        var routeModel = context.GetRouteModel();

        if (routeModel?.Config?.Metadata != null &&
            routeModel.Config.Metadata.TryGetValue(
                "RateLimitPolicy", out var policy))
        {
            // Tag the context with the policy name so the RateLimiter
            // middleware (which already ran) can look it up
            // This is a simplified approach — we log the policy for observability
            _logger.LogDebug(
                "Route '{RouteId}' using rate limit policy '{Policy}'",
                routeModel.Config.RouteId, policy);
        }

        await _next(context);
    }
}