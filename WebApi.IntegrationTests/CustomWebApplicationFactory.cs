using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataModel.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private PostgreSqlContainer _postgresContainer;
    private RabbitMqContainer _rabbitMqContainer;
    private string _postgresHost;
    private int _postgresPort;
    private string _rabbitHost;
    private int _rabbitPort;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configurationValues = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", $"Host={_postgresHost};Port={_postgresPort};Username=postgres;Password=mysecretpassword;Database=mydatabase"},
            {"RabbitMQ:Host", _rabbitHost},
            {"RabbitMQ:Port", _rabbitPort.ToString()},
            {"RabbitMQ:UserName", "guest"},
            {"RabbitMQ:Password", "guest"},
            {"queueName", "C1"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        builder.UseConfiguration(configuration)
               .ConfigureAppConfiguration(configurationBuilder =>
               {
                   configurationBuilder.AddInMemoryCollection(configurationValues);
               });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AbsanteeContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<AbsanteeContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddDbContextFactory<AbsanteeContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            }, ServiceLifetime.Scoped);
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("mydatabase")
            .WithUsername("postgres")
            .WithPassword("mysecretpassword")
            .WithImage("postgres:latest")
            .WithCleanUp(true)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .Build();

        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        _postgresHost = _postgresContainer.Hostname;
        _postgresPort = _postgresContainer.GetMappedPublicPort(5432);

        _rabbitHost = _rabbitMqContainer.Hostname;
        _rabbitPort = _rabbitMqContainer.GetMappedPublicPort(5672);

        // Ensure RabbitMQ is ready
        await WaitForRabbitMqAsync(_rabbitHost, _rabbitPort);

        await Task.Delay(10000); // Ensure containers are fully ready
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
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }

        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.DisposeAsync();
        }
    }
}
