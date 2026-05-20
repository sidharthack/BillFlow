namespace BillFlow.Gateway.Middleware;

/// <summary>
/// Adds security headers to every response passing through the gateway.
/// Services don't need to add these — the gateway handles it centrally.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register a callback to add headers just before response starts
        // (can't add headers after body has started writing)
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Block clickjacking — page can't be embedded in an iframe
            headers["X-Frame-Options"] = "DENY";

            // Enable browser XSS protection (legacy browsers)
            headers["X-XSS-Protection"] = "1; mode=block";

            // Control what info is sent in the Referer header
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Strict Transport Security — enforce HTTPS (1 year)
            // Only meaningful in production with real HTTPS
            headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains";

            // Content Security Policy — restrict what resources can load
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'";

            // Remove server info — don't reveal what's behind the gateway
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}