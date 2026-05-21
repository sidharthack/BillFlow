using BillFlow.NotificationService.Consumers;
using BillFlow.NotificationService.Data;
using BillFlow.NotificationService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();