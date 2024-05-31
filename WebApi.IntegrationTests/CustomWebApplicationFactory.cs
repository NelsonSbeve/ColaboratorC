using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DataModel.Repository;
using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
       private RabbitMqContainer _rabbitMqContainer;
  
    private string _rabbitHost;
    private int _rabbitPort;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

      var configurationValues = new Dictionary<string, string>
        {
            { "queueName", "C1" },
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        builder.UseConfiguration(configuration)
               .ConfigureAppConfiguration(configurationBuilder =>
               {
                   configurationBuilder.AddInMemoryCollection(configurationValues);
               });

        builder.ConfigureServices((context, services) =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AbsanteeContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                return connection;
            });

            services.AddDbContext<AbsanteeContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });

        builder.UseEnvironment("Development");
    
    }

    public async Task InitializeAsync()
    {
  
         TestcontainersSettings.ResourceReaperEnabled = false;

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .Build();

      
        await _rabbitMqContainer.StartAsync();

        

        _rabbitHost = _rabbitMqContainer.Hostname;
        _rabbitPort = _rabbitMqContainer.GetMappedPublicPort(5672);

        // Ensure RabbitMQ is ready
        await WaitForRabbitMqAsync(_rabbitHost, _rabbitPort);

        await Task.Delay(10000);
        
        Environment.SetEnvironmentVariable("RABBITMQ_PORT", _rabbitPort.ToString());
        Environment.SetEnvironmentVariable("RABBITMQ_HOSTNAME", _rabbitHost);
        Environment.SetEnvironmentVariable("RABBITMQ_USERNAME", "guest");
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "guest"); // Ensure containers are fully ready
    }

    private async Task WaitForRabbitMqAsync(string host, int port)
    {
        var factory = new ConnectionFactory() { HostName = host, Port = port, UserName = "guest", Password = "guest" };
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using (var connection = factory.CreateConnection())
                {
                    if (connection.IsOpen)
                    {
                        return;
                    }
                }
            }
            catch
            {
                await Task.Delay(1000); // Wait for 1 second before retrying
            }
        }
        throw new Exception("RabbitMQ is not ready");
    }

    public async Task DisposeAsync()
    {
    
        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.DisposeAsync();
        }
    }
}
