namespace Application.Services
{
    public interface IRabbitMQConsumerController
    {
        public void StartConsuming();
        void ConfigQueue(string queueName);
    }
}