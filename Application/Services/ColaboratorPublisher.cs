using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Application.Services
{
   
    public class ColaboratorPublisher
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly string _queueName;
        private readonly IModel channel;

        public ColaboratorPublisher(IConfiguration configuration, IConnectionFactory factory)
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"], // Ensure correct key name
                Port = int.Parse(configuration["RabbitMQ:Port"]), // Ensure correct key name
                UserName = configuration["RabbitMQ:UserName"], // Ensure correct key name
                Password = configuration["RabbitMQ:Password"]
            };
           _queueName = configuration["RabbitMQ:QueueName"] ?? throw new InvalidOperationException("Queue name is null. Please check configuration.");
           
            var connection = _connectionFactory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "colab_logs", type: ExchangeType.Fanout);
        
        }
   

       

        public void PublishMessage(string message)
        {
          
          

            var body = Encoding.UTF8.GetBytes(message);
            
            channel.BasicPublish(exchange: "colab_logs",
                              routingKey: "colabKey",
                              basicProperties: null,
                              body: body);
            

           
        }
    }
}