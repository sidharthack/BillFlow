using System.Text;
using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Middleware;
using BillFlow.InvoiceService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);

// JWT settings — registered as singleton so middleware can resolve it
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

builder.Services.AddSingleton(jwtSettings);

// ASP.NET Core JWT authentication pipeline
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

// Tenant context — scoped per request
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp =>
    sp.GetRequiredService<TenantContext>());

// HttpClient for TenantService
builder.Services.AddHttpClient("TenantService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:TenantService"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Business services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

var app = builder.Build();


app.UseHttpsRedirection();

// Order is critical:
// 1. Logging (sees everything)
// 2. Authentication (validates JWT signature)
// 3. TenantMiddleware (reads claims, populates ITenantContext)
// 4. Authorization (enforces [Authorize] attributes)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();