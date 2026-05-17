using BillFlow.Contracts.Tenancy;
using BillFlow.InvoiceService.Data;
using BillFlow.InvoiceService.Middleware;
using BillFlow.InvoiceService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
    )
);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddHttpClient("TenantService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:TenantService"] ?? "https://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Business services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

var app = builder.Build();


// Order matters — TenantMiddleware must run before controllers
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantMiddleware>();         // ← ADD
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();