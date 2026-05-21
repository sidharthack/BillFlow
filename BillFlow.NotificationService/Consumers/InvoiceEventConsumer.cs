using System.Text;
using System.Text.Json;
using BillFlow.Contracts.Events;
using BillFlow.NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BillFlow.NotificationService.Consumers;

/// <summary>
/// Long-running background service that consumes invoice events
/// from RabbitMQ and dispatches them to NotificationService.
/// Runs for the lifetime of the application.
/// </summary>
public class InvoiceEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<InvoiceEventConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "billflow.events";
    private const string QueueName = "billflow.notifications";

    // Routing keys this service cares about
    private static readonly string[] RoutingKeys =
    [
        "invoice.created",
        "invoice.sent",
        "invoice.overdue",
        "invoice.paid"
    ];

    public InvoiceEventConsumer(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<InvoiceEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect with retry — RabbitMQ might not be ready on startup
        await ConnectWithRetryAsync(stoppingToken);

        if (_channel is null) return;

        // Declare the queue and bind to all invoice routing keys
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,          // survives RabbitMQ restart
            exclusive: false,       // shared across multiple consumer instances
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                // Dead letter queue — failed messages go here
                ["x-dead-letter-exchange"] = "billflow.dead-letters",
                ["x-message-ttl"] = 86400000   // 24 hours TTL
            });

        // Bind to each routing key
        foreach (var key in RoutingKeys)
        {
            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: key);
        }

        // Declare dead letter exchange for failed messages
        await _channel.ExchangeDeclareAsync(
            exchange: "billflow.dead-letters",
            type: ExchangeType.Topic,
            durable: true);

        _logger.LogInformation(
            "NotificationService subscribed to queue '{Queue}' " +
            "with keys: {Keys}",
            QueueName, string.Join(", ", RoutingKeys));

        // Set prefetch — process one message at a time
        // Prevents memory overload if thousands of events arrive at once
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        // Create async consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        // Start consuming — autoAck: false means we manually acknowledge
        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        // Keep running until app shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceivedAsync(
        object sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var messageId = ea.BasicProperties.MessageId ?? "unknown";
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        _logger.LogInformation(
            "Received event '{RoutingKey}' [msgId: {MessageId}]",
            routingKey, messageId);

        try
        {
            // Use a scoped service provider — NotificationService is Scoped
            // BackgroundService is Singleton so we must create a scope manually
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();

            await DispatchAsync(notificationService, routingKey, body);

            // Acknowledge — remove message from queue
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);

            _logger.LogInformation(
                "Processed event '{RoutingKey}' [msgId: {MessageId}]",
                routingKey, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process event '{RoutingKey}' [msgId: {MessageId}]",
                routingKey, messageId);

            // Negative acknowledge — requeue: false sends to dead letter queue
            // after max retries (configured in queue args above)
            await _channel!.BasicNackAsync(
                ea.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }

    private static async Task DispatchAsync(
        INotificationService service,
        string routingKey,
        string body)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        switch (routingKey)
        {
            case "invoice.created":
                var created = JsonSerializer.Deserialize<InvoiceCreatedEvent>(body, opts)
                    ?? throw new InvalidOperationException("Failed to deserialize InvoiceCreatedEvent");
                await service.HandleInvoiceCreatedAsync(created);
                break;

            case "invoice.sent":
                var sent = JsonSerializer.Deserialize<InvoiceSentEvent>(body, opts)
                    ?? throw new InvalidOperationException("Failed to deserialize InvoiceSentEvent");
                await service.HandleInvoiceSentAsync(sent);
                break;

            case "invoice.overdue":
                var overdue = JsonSerializer.Deserialize<InvoiceOverdueEvent>(body, opts)
                    ?? throw new InvalidOperationException("Failed to deserialize InvoiceOverdueEvent");
                await service.HandleInvoiceOverdueAsync(overdue);
                break;

            case "invoice.paid":
                var paid = JsonSerializer.Deserialize<InvoicePaidEvent>(body, opts)
                    ?? throw new InvalidOperationException("Failed to deserialize InvoicePaidEvent");
                await service.HandleInvoicePaidAsync(paid);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown routing key '{routingKey}'");
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"] ?? "localhost",
            Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
            UserName = _config["RabbitMq:Username"] ?? "billflow",
            Password = _config["RabbitMq:Password"] ?? "billflow123",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        var attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempts++;
                _connection = await factory.CreateConnectionAsync(
                    "NotificationService", stoppingToken);
                _channel = await _connection.CreateChannelAsync(
                    cancellationToken: stoppingToken);

                _logger.LogInformation(
                    "RabbitMQ consumer connected on attempt {Attempt}", attempts);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "RabbitMQ connection attempt {Attempt}/{Max} failed: {Message}",
                    attempts, maxAttempts, ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(5 * attempts), stoppingToken);
            }
        }

        _logger.LogError(
            "Could not connect to RabbitMQ after {Max} attempts", maxAttempts);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.CloseAsync();

        if (_connection is not null)
            await _connection.CloseAsync();
    }
}