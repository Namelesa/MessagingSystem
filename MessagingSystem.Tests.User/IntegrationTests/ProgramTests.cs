using Microsoft.AspNetCore.Mvc.Testing;
using Program = MessagingSystem.Services.User.WebApi.Program;

namespace MessagingSystem.Tests.User.IntegrationTests
{
    public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Swagger_Endpoint_ReturnsSuccess()
        {
            var response = await _client.GetAsync("/swagger/index.html");
            response.EnsureSuccessStatusCode();
        }
    }
}