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
       
        private readonly string _queueName;
        private readonly IModel channel;
        private readonly IConnectionFactory _factory;

        public ColaboratorPublisher(IConfiguration configuration, IConnectionFactory factory)
        {
           
            _factory = factory;

            var connection = factory.CreateConnection();
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