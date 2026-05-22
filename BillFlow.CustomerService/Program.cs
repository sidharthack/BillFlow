using BillFlow.Contracts.Logging;
using BillFlow.Contracts.Tenancy;
using BillFlow.CustomerService.Data;
using BillFlow.CustomerService.Middleware;
using BillFlow.CustomerService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
const string ServiceName = "CustomerService";
// Use: "TenantService", "IdentityService", "CustomerService",
//      "NotificationService" in respective services

SerilogBootstrap.Configure(ServiceName);
try
{
    var builder = WebApplication.CreateBuilder(args);
    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Database
    builder.Services.AddDbContext<CustomerDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );

    // JWT
    var jwtSettings = builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()!;

    builder.Services.AddSingleton(jwtSettings);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    // Tenant context
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp =>
        sp.GetRequiredService<TenantContext>());

    // HttpClient for TenantService
    builder.Services.AddHttpClient("TenantService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ServiceUrls:TenantService"] ?? "https://localhost:5001");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    // Business services
    builder.Services.AddScoped<ICustomerService, CustomerService>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseMiddleware<ServiceCorrelationMiddleware>();

    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();
    app.MapControllers();

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