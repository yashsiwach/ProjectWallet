using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reward_Service.DTOs;
using Reward_Service.Services;
using System.Text;
using System.Text.Json;

namespace Reward_Service.Services;


public class TransferCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TransferCompletedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public TransferCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<TransferCompletedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

   
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Connect to RabbitMQ
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName = _config["RabbitMQ:User"] ?? "guest",
                Password = _config["RabbitMQ:Pass"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // queue we want to listen to
            _channel.QueueDeclare(
                queue: "transfer_completed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Only process one message at a time
            _channel.BasicQos(0, 1, false);


            // Create consumer
            var consumer = new EventingBasicConsumer(_channel);

           
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    // Read message from queue
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

              
                    var payload = JsonSerializer.Deserialize<TransferEvent>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (payload != null)
                    {
                        
                        using var scope = _scopeFactory.CreateScope();
                        var rewardService = scope.ServiceProvider.GetRequiredService<RewardServices>();

                        // Award points
                        await rewardService.AwardPointsAsync(new AwardPointsRequest
                        {
                            UserId = payload.SenderUserId,
                            Reference = payload.Reference + "_OUT",
                            Reason = "transfer_completed"
                        });
                    }
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {

                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            
            _channel.BasicConsume(
                queue: "transfer_completed",
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

// Shape of the message we expect from WalletService
internal class TransferEvent
{
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}