using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Application.DTO;
using DataModel.Repository;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using WebApi.IntegrationTests.Helpers;
using Xunit;

namespace WebApi.IntegrationTests.Tests
{
    public class ColaboratorControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ColaboratorControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
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
        public async Task PostDuplicate_EndpointReturnsBadRequest(string url)
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
