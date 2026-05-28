using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillFlow.Contracts.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Logs all configuration keys on startup — useful for debugging
    /// missing env vars in containers. Only logs in Development.
    /// </summary>
    public static void LogConfiguration(
        this IConfiguration config,
        ILogger logger,
        string environment)
    {
        if (environment != "Development") return;

        var keys = new[]
        {
            "ConnectionStrings:DefaultConnection",
            "JwtSettings:SecretKey",
            "RabbitMq:Host",
            "Seq:Url",
        };

        foreach (var key in keys)
        {
            var value = config[key];
            var masked = key.Contains("Password") || key.Contains("Secret") || key.Contains("Key")
                ? (value is not null ? "***SET***" : "NOT SET")
                : (value ?? "NOT SET");

            logger.LogInformation(
                "Config [{Key}] = {Value}", key, masked);
        }
    }
}