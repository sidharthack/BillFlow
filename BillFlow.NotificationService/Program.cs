using BillFlow.Contracts.Logging;
using BillFlow.NotificationService.Consumers;
using BillFlow.NotificationService.Data;
using BillFlow.NotificationService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string ServiceName = "NotificationService";
// Use: "TenantService", "", "CustomerService",
//      "NotificationService" in respective services

SerilogBootstrap.Configure(ServiceName);
try
{
    var builder = WebApplication.CreateBuilder(args);
    SerilogBootstrap.ConfigureBuilder(builder, ServiceName);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Database
    builder.Services.AddDbContext<NotificationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
        )
    );

    // Services
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // RabbitMQ consumer — runs for app lifetime
    builder.Services.AddHostedService<InvoiceEventConsumer>();

    var app = builder.Build();

    app.UseMiddleware<ServiceCorrelationMiddleware>();

    app.UseHttpsRedirection();
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