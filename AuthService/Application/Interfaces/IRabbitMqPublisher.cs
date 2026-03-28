namespace AuthService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    void Publish<T>(string queueName, T message);
}
