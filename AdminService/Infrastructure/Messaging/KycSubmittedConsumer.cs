using AdminService.Application.Interfaces;
using AdminService.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AdminService.Infrastructure.Messaging;

public class KycSubmittedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<KycSubmittedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public KycSubmittedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<KycSubmittedConsumer> logger)
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

            _channel.QueueDeclare(queue: "kyc_submitted", durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.BasicQos(0, 1, false);

            _logger.LogInformation("AdminService listening to kyc_submitted queue");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("KYC event received: {Json}", json);

                    var payload = JsonSerializer.Deserialize<KycSubmittedEvent>(
                        json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (payload != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();

                        await adminService.SyncKycAsync(new SyncKycRequest
                        {
                            UserId = payload.UserId,
                            UserFullName = payload.UserFullName,
                            UserEmail = payload.UserEmail,
                            DocumentType = payload.DocumentType,
                            DocumentNumber = payload.DocumentNumber,
                            SubmittedAt = payload.SubmittedAt
                        });
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing KYC event: {Message}", ex.Message);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue: "kyc_submitted", autoAck: false, consumer: consumer);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not connect to RabbitMQ: {Message}", ex.Message);
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

// Internal event payload — kept in messaging layer
internal class KycSubmittedEvent
{
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
