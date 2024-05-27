using System.Net;
using System.Text;
using System.Threading.Tasks;
using Application.DTO;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
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
            var colaboratorDTO = new ColaboratorDTO
            {
                Name = "", // Invalid name
                Email = "invalid-email", // Invalid email
                Street = "Test Street",
                PostalCode = "12345"
            };
            var jsonContent = JsonConvert.SerializeObject(colaboratorDTO);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(url, content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // Other test methods here...
    }
}
