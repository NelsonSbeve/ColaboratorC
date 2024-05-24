using Microsoft.EntityFrameworkCore;

using Application.Services;
using DataModel.Repository;
using DataModel.Mapper;
using Domain.Factory;
using Domain.IRepository;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Microsoft.OpenApi.Any;


var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

string queueNameArg =  Array.Find(args, arg => arg.Contains("--queueName"));
string queueName;

if (queueNameArg != null)
    queueName = queueNameArg.Split('=')[1];
   
else
    queueName = config.GetConnectionString("queueName");



 var port = GetPortForQueue(queueName);
 
// Add services to the container.
            var HostName = config["RabbitMQ:HostName"]; // Ensure correct key name
            var Port = int.Parse(config["RabbitMQ:Port"]); // Ensure correct key name
            var UserName = config["RabbitMQ:UserName"]; // Ensure correct key name
            var Password = config["RabbitMQ:Password"];



builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    return new ConnectionFactory()
    {
        HostName = HostName,
        Port = Port,
        UserName = UserName,
        Password = Password
    };
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AbsanteeContext>(opt =>
    //opt.UseInMemoryDatabase("AbsanteeList")
    //opt.UseSqlite("Data Source=AbsanteeDatabase.sqlite")
     opt.UseSqlite(Host.CreateApplicationBuilder().Configuration.GetConnectionString(queueName))
    );

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
    opt.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Example = new OpenApiString(DateTime.Today.ToString("yyyy-MM-dd"))
    })
);


builder.Services.AddTransient<IColaboratorRepository, ColaboratorRepository>();
builder.Services.AddTransient<IColaboratorFactory, ColaboratorFactory>();
builder.Services.AddTransient<ColaboratorMapper>();
builder.Services.AddTransient<ColaboratorService>();
builder.Services.AddTransient<ColaboratorPublisher>();
builder.Services.AddSingleton<IRabbitMQConsumerController, ColaboratorConsumer>();
// builder.Services.AddSingleton<IRabbitMQConsumerController, HolidayValidationConsumer>();







var app = builder.Build();

var rabbitMQConsumerServices = app.Services.GetServices<ColaboratorConsumer>();
foreach (var service in rabbitMQConsumerServices)
{
    service.ConfigQueue(queueName);
    service.StartConsuming();
};

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); 

app.UseAuthorization();


app.UseCors("AllowAllOrigins");

app.MapControllers();


var rabbitMQConsumerService = app.Services.GetRequiredService<IRabbitMQConsumerController>();
rabbitMQConsumerService.ConfigQueue(queueName);
rabbitMQConsumerService.StartConsuming();

// var rabbitMQConsumerServiceHoliday = app.Services.GetRequiredService<IHolidayValidationConsumer>();
// rabbitMQConsumerServiceHoliday.StartHolidayConsuming(queueName);
 

app.Run($"https://localhost:{port}");

static int GetPortForQueue(string queueName)
{
    // Implement logic to map queue name to a unique port number
    // Example: Assign a unique port number based on the queue name suffix
    int basePort = 5010; // Start from port 5000
    int queueIndex = int.Parse(queueName.Substring(1)); // Extract the numeric part of the queue name (assuming it starts with 'C')
    return basePort + queueIndex;
}

public partial class Program { }