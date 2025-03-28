using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TransactionService.Models;
using Xunit;

namespace Integration.Tests
{
    public class TransactionControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public TransactionControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Mock services if needed
                });
            });

            _client = _factory.CreateClient();

            // Get a valid JWT token for testing
            var token = GetJwtToken();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private string GetJwtToken()
        {
            // In a real scenario, you would call your auth service
            // This is a mock token for testing
            return "your-test-jwt-token";
        }

        [Fact]
        public async Task PurchaseAirtime_ReturnsSuccess_WithValidRequest()
        {
            // Arrange
            var request = new AirtimePurchaseRequest
            {
                PhoneNumber = "1234567890",
                Amount = 100,
                Provider = "Capricorn",
                IdempotencyKey = "test-key-1"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/transaction/airtime/purchase", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Transaction>(responseString);

            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task PurchaseAirtime_ReturnsBadRequest_WithInvalidAmount()
        {
            // Arrange
            var request = new AirtimePurchaseRequest
            {
                PhoneNumber = "1234567890",
                Amount = -100, // Invalid amount
                Provider = "Capricorn"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/transaction/airtime/purchase", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}