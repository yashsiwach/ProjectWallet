using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AuthService.Services;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IConfiguration config,
        ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                UserName = config["RabbitMQ:User"] ?? "guest",
                Password = config["RabbitMQ:Pass"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("RabbitMQ connected in AuthService.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ connection failed: {Message}", ex.Message);
            _connection = null!;
            _channel = null!;
        }
    }

    public void Publish<T>(string queueName, T message)
    {
        if (_channel == null) return;

        try
        {
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;

            _channel.BasicPublish("", queueName, props, body);

            _logger.LogInformation("Published to {Queue}: {Message}", queueName, json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Publish failed: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}