using System.Text;
using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class ColaboratorConsumer : IRabbitMQConsumerController
{
    private readonly IModel _channel;
   

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnectionFactory _factory;

    private string _queueName;
  

    List<string> _errorMessages = new List<string>();

    public ColaboratorConsumer(IServiceScopeFactory serviceScopeFactory, IConnectionFactory factory)
    {
       
        _serviceScopeFactory = serviceScopeFactory;
        _factory = factory;
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();

        _channel.ExchangeDeclare(exchange: "colab_logs", type: ExchangeType.Fanout);

        

        // _queueName =_channel.QueueDeclare().QueueName;
        // _channel.QueueBind(queue: _queueName, exchange: "colab_logs", routingKey: "colabKey");
        Console.WriteLine(" [*] Waiting for Collaborator messages.");
    }

        public void ConfigQueue(string queueName)
        {
            _queueName = queueName;

            _channel.QueueDeclare(queue: _queueName,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);

            _channel.QueueBind(queue: _queueName,
                  exchange: "colab_logs",
                  routingKey: "colabKey");
        }

    public void StartConsuming()
    {
        


        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var colaboratorResult = JsonConvert.DeserializeObject<ColaboratorDTO>(message);

            var colaboratorDTO = new ColaboratorDTO
            {
                Id = colaboratorResult.Id,
                Name = colaboratorResult.Name,
                Email = colaboratorResult.Email,                    
                Street = colaboratorResult.Street,
                PostalCode = colaboratorResult.PostalCode,
            };
            
            using (var scope = _serviceScopeFactory.CreateScope()){
            var colaboratorService = scope.ServiceProvider.GetRequiredService<ColaboratorService>();
            await colaboratorService.Add(colaboratorDTO, _errorMessages);
            }
            Console.WriteLine($" [ColaboratorConsumer] {message}");
 
        };

        _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
    }
}
