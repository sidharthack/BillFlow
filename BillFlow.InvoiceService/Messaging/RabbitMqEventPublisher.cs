using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace BillFlow.InvoiceService.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private const string ExchangeName = "billflow.events";

    // Private constructor — use CreateAsync factory
    private RabbitMqEventPublisher(
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    /// <summary>
    /// Factory method — async creation required in RabbitMQ.Client v7+
    /// </summary>
    public static async Task<RabbitMqEventPublisher> CreateAsync(
        IConfiguration configuration,
        ILogger<RabbitMqEventPublisher> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
            UserName = configuration["RabbitMq:Username"] ?? "billflow",
            Password = configuration["RabbitMq:Password"] ?? "billflow123",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        var connection = await factory.CreateConnectionAsync("InvoiceService");
        var channel = await connection.CreateChannelAsync();

        // Declare topic exchange
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        logger.LogInformation(
            "RabbitMQ publisher connected to {Host}", factory.HostName);

        return new RabbitMqEventPublisher(connection, channel, logger);
    }

    public async Task PublishAsync<T>(T eventMessage, string routingKey) where T : class
    {
        var json = JsonSerializer.Serialize(eventMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = typeof(T).Name
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Published {EventType} with routing key '{RoutingKey}'",
            typeof(T).Name, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        GC.SuppressFinalize(this);
    }
}