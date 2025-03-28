using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Polly.Retry;
using TransactionService.Interfaces;
using TransactionService.Models;

namespace TransactionService.Services
{
    public class CapricornService : ICapricornService
    {
        private readonly HttpClient _httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILogger<CapricornService> _logger;
        private readonly IConfiguration _configuration;

        public CapricornService(
            HttpClient httpClient,
            ILogger<CapricornService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Configure retry policy with exponential backoff
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(x => (int)x.StatusCode >= 500)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, delay, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} for failed request. Delaying for {delay.TotalMilliseconds}ms");
                    });
        }

        public async Task<Transaction> PurchaseAirtime(AirtimePurchaseRequest request)
        {
            var endpoint = "/airtime/purchase";
            var payload = new
            {
                phoneNumber = request.PhoneNumber,
                amount = request.Amount,
                reference = Guid.NewGuid().ToString()
            };

            return await ExecuteTransactionRequest(endpoint, payload, "Airtime");
        }

        public async Task<Transaction> PurchaseData(DataPurchaseRequest request)
        {
            var endpoint = "/data/purchase";
            var payload = new
            {
                phoneNumber = request.PhoneNumber,
                dataPlanId = request.DataPlanId,
                reference = Guid.NewGuid().ToString()
            };

            var response = await ExecuteTransactionRequest(endpoint, payload, "Data");

            // Parse the response to get the amount
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CapricornDataResponse>(responseContent);

            return new Transaction
            {
                Status = "Completed",
                Reference = result.Reference,
                TransactionType = "Data",
                Provider = "Capricorn",
                Amount = result.Amount
            };
        }

        public async Task<Transaction> SubscribeTv(TvSubscriptionRequest request)
        {
            var endpoint = "/tv/subscribe";
            var payload = new
            {
                smartCardNumber = request.SmartCardNumber,
                subscriptionPlanId = request.SubscriptionPlanId,
                reference = Guid.NewGuid().ToString()
            };

            var response = await ExecuteTransactionRequest(endpoint, payload, "TV");

            // Parse the response to get the amount
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CapricornTvResponse>(responseContent);

            return new Transaction
            {
                Status = "Completed",
                Reference = result.Reference,
                TransactionType = "TV",
                Provider = "Capricorn",
                Amount = result.Amount
            };
        }

        private async Task<Transaction> ExecuteTransactionRequest(string endpoint, object payload, string transactionType)
        {
            try
            {
                // Add authentication headers
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _configuration["CapricornApi:ApiKey"]);

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // Execute with retry policy
                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.PostAsync(endpoint, content));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Capricorn API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Capricorn API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CapricornApiResponse>(responseContent);

                return new Transaction
                {
                    Status = "Completed",
                    Reference = result.Reference,
                    TransactionType = transactionType,
                    Provider = "Capricorn"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Capricorn API request");
                throw;
            }
        }

        private class CapricornApiResponse
        {
            public string Reference { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
        }
    }
}