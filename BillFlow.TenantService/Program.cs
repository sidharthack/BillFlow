using BillFlow.Contracts.Logging;
using BillFlow.TenantService.Data;
using BillFlow.TenantService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string ServiceName = "TenantService";
// Use: "TenantService", "", "CustomerService",
//      "NotificationService" in respective services

SerilogBootstrap.Configure(ServiceName);
try
{

    var builder = WebApplication.CreateBuilder(args);
    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddDbContext<TenantDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );

    builder.Services.AddScoped<ITenantService, TenantService>();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseMiddleware<ServiceCorrelationMiddleware>();

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