using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using Application.DTO;
using Application.Services;
using DataModel.Model;
using DataModel.Repository;
using Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using WebApi.IntegrationTests.Helpers;
using Xunit;

namespace WebApi.IntegrationTests.Tests
{
    public class ColaboratorControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ColaboratorControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }




        [Theory]
        [InlineData("/api/colaborator")]
        public async Task Post_EndpointReturnsBadRequestOnInvalidData(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            var colaboratorDTO = new ColaboratorDTO
            {
                Name = "", // Invalid name
                Email = "invalid-email", // Invalid email
                Street = "Test Street",
                PostalCode = "12345"
            };
            var jsonContent = JsonConvert.SerializeObject(colaboratorDTO);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/colaborator")]
        public async Task Post_EndpointResponseTimeIsAcceptable(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            var colaboratorDTO = new ColaboratorDTO
            {
                Name = "Test Name",
                Email = "test@example.com",
                Street = "Test Street",
                PostalCode = "12345"
            };
            var jsonContent = JsonConvert.SerializeObject(colaboratorDTO);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var maxResponseTime = TimeSpan.FromSeconds(3); // Define maximum acceptable response time

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.PostAsync(url, content);
            stopwatch.Stop();
            

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.True(stopwatch.Elapsed < maxResponseTime, $"Response time exceeded {maxResponseTime.TotalSeconds} seconds");
        }


        [Theory]
        [InlineData("/api/colaborator")]
        public async Task PostDuplicate_EndPointReturnError(string url)
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AbsanteeContext>();
    
                Utilities.ReinitializeDbForTests(db);
                Utilities.InitializeDbForTests(db);
            }

            var client = _factory.CreateClient();

            // Create a sample ColaboratorDTO object to be posted
            var colaboratorDTO = new ColaboratorDTO
            {
                Name = "Test Name",
                Email = "a@email.pt",
                Street = "Test Street",
                PostalCode = "12345"
            };

            // Serialize the ColaboratorDTO object to JSON and set the content type
            var jsonContent = JsonConvert.SerializeObject(colaboratorDTO);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content); 

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // Status Code 200-299
            
        }
        
        [Theory]
        [InlineData("/api/colaborator")]
        public async Task Post_EndpointReturnsSuccess(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Create a sample ColaboratorDTO object to be posted
            var colaboratorDTO = new ColaboratorDTO
            {
                Name = "Test Name",
                Email = "tesasd@example.com",
                Street = "Test Street",
                PostalCode = "12345"
            };

            // Serialize the ColaboratorDTO object to JSON and set the content type
            var jsonContent = JsonConvert.SerializeObject(colaboratorDTO);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
        }
    
    }


}