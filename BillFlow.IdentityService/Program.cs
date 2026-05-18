using BillFlow.IdentityService.Data;
using BillFlow.IdentityService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);

// JWT settings — binds appsettings.json JwtSettings section
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// HttpClient for TenantService
builder.Services.AddHttpClient("TenantService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:TenantService"] ?? "https://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();