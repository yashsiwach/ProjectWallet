using Microsoft.Identity.Client;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace WalletService.Services
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;


        public RabbitMqPublisher(IConfiguration config)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = config["RabbitMQ:HostName"] ?? "localhost",
                    Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                    UserName = config["RabbitMQ:UserName"] ?? "guest",
                    Password = config["RabbitMQ:Password"] ?? "guest"
                };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch (Exception ex)
            {
                _connection = null!;
                _channel = null!;
            }
        }
        public void Publish<T>(string queueName, T message)
        {
            if (_channel == null) { return; }
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

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: props,
                    body: body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


        }
        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
   
   