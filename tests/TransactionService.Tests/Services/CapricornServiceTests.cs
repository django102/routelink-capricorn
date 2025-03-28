using Moq;
using Moq.Protected;
using System.Net;
using TransactionService.Interfaces;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Tests.Services
{
    public class CapricornServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<CapricornService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CapricornService _service;

        public CapricornServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockLogger = new Mock<ILogger<CapricornService>>();
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(x => x["CapricornApi:ApiKey"]).Returns("test-api-key");

            _service = new CapricornService(_httpClient, _mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task PurchaseAirtime_ShouldReturnTransaction_OnSuccess()
        {
            // Arrange
            var request = new AirtimePurchaseRequest
            {
                PhoneNumber = "1234567890",
                Amount = 100
            };

            var expectedResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"reference\":\"ref123\",\"status\":\"success\"}",
                    Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.PurchaseAirtime(request);

            // Assert
            Assert.Equal("Completed", result.Status);
            Assert.Equal("ref123", result.Reference);
            Assert.Equal("Capricorn", result.Provider);
        }

        [Fact]
        public async Task PurchaseAirtime_ShouldRetry_OnFailure()
        {
            // Arrange
            var request = new AirtimePurchaseRequest
            {
                PhoneNumber = "1234567890",
                Amount = 100
            };

            var failureResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            };

            var successResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"reference\":\"ref123\",\"status\":\"success\"}",
                    Encoding.UTF8, "application/json")
            };

            var sequence = new MockSequence();
            _mockHttpMessageHandler.Protected()
                .InSequence(sequence)
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(failureResponse);

            _mockHttpMessageHandler.Protected()
                .InSequence(sequence)
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(successResponse);

            // Act
            var result = await _service.PurchaseAirtime(request);

            // Assert
            Assert.Equal("Completed", result.Status);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retry")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}