using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Services;

public class NotificationConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public NotificationConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<NotificationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName = _config["RabbitMQ:User"] ?? "guest",
                Password = _config["RabbitMQ:Pass"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Only listen to notifications queue
            _channel.QueueDeclare(
                queue: "notifications",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(0, 1, false);

            _logger.LogInformation(
                "NotificationService listening to notifications queue");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("Notification received: {Json}", json);

                    var payload = JsonSerializer.Deserialize<NotificationEvent>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (payload != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var service = scope.ServiceProvider
                            .GetRequiredService<NotificationServices>();

                        await service.SaveNotificationAsync(
                            payload.UserId,
                            payload.Title,
                            payload.Message,
                            payload.Type);
                    }

                    _channel!.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "Error processing notification: {Message}", ex.Message);
                    _channel!.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: "notifications",
                autoAck: false,
                consumer: consumer);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Could not connect to RabbitMQ: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

// Shape of notification events from any service
internal class NotificationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}