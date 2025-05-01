using Microsoft.AspNetCore.Mvc.Testing;
using Program = MessagingSystem.Services.Notification.WebApi.Program;

namespace MessagingSystem.Tests.Notification.IntegrationTests
{
    public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Swagger_Endpoint_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/swagger");
            response.EnsureSuccessStatusCode();
        }
    }
}