using FraudDetectionService.Models;

namespace FraudDetectionService.Interfaces
{
    public interface IFraudDetectionService
    {
        Task<FraudDetectionResult> EvaluateTransaction(TransactionEvaluationRequest request);
    }
}

// src/FraudDetectionService/Services/FraudDetectionService.cs
using FraudDetectionService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FraudDetectionService.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly ITransactionService _transactionService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<FraudDetectionService> _logger;

        public FraudDetectionService(
            ITransactionService transactionService,
            ICacheService cacheService,
            ILogger<FraudDetectionService> logger)
        {
            _transactionService = transactionService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<FraudDetectionResult> EvaluateTransaction(TransactionEvaluationRequest request)
        {
            var result = new FraudDetectionResult();
            
            // Rule 1: Unusually large transaction amount
            if (request.Amount > 10000) // Threshold can be configurable
            {
                result.FraudScore += 30;
                result.Reason += "Large transaction amount;";
            }

            // Rule 2: High frequency of transactions
            var userTransactions = await _transactionService.GetRecentUserTransactions(request.UserId);
            if (userTransactions.Count(t => t.TransactionDate > DateTime.UtcNow.AddHours(-1)) > 5)
            {
                result.FraudScore += 25;
                result.Reason += "High transaction frequency;";
            }

            // Rule 3: Transaction to new recipient
            if (!userTransactions.Any(t => t.Recipient == request.Recipient))
            {
                result.FraudScore += 20;
                result.Reason += "New recipient;";
            }

            // Rule 4: Unusual time for user (based on historical patterns)
            var userPattern = await GetUserPattern(request.UserId);
            if (userPattern != null && !IsUsualTime(userPattern))
            {
                result.FraudScore += 15;
                result.Reason += "Unusual transaction time;";
            }

            // Rule 5: Velocity check (sudden increase in spending)
            var dailySpending = userTransactions
                .Where(t => t.TransactionDate > DateTime.UtcNow.AddDays(-1))
                .Sum(t => t.Amount);
                
            if (dailySpending + request.Amount > userPattern?.AverageDailySpending * 3)
            {
                result.FraudScore += 10;
                result.Reason += "Unusual spending pattern;";
            }

            result.IsFraudulent = result.FraudScore > 50; // Threshold can be configurable
            return result;
        }

        private async Task<UserTransactionPattern> GetUserPattern(string userId)
        {
            var cacheKey = $"user_pattern_{userId}";
            var cachedPattern = await _cacheService.GetCacheAsync<string>(cacheKey);
            
            if (cachedPattern != null)
            {
                return JsonSerializer.Deserialize<UserTransactionPattern>(cachedPattern);
            }

            // Calculate pattern from historical transactions
            var transactions = await _transactionService.GetUserTransactionHistory(userId);
            var pattern = new UserTransactionPattern
            {
                UserId = userId,
                AverageDailySpending = transactions.Average(t => t.Amount),
                CommonTransactionTimes = transactions
                    .GroupBy(t => t.TransactionDate.Hour)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList()
            };

            await _cacheService.SetCacheAsync(cacheKey, 
                JsonSerializer.Serialize(pattern), 
                TimeSpan.FromHours(1));

            return pattern;
        }

        private bool IsUsualTime(UserTransactionPattern pattern)
        {
            return pattern.CommonTransactionTimes.Contains(DateTime.UtcNow.Hour);
        }
    }

    public class UserTransactionPattern
    {
        public string UserId { get; set; }
        public decimal AverageDailySpending { get; set; }
        public List<int> CommonTransactionTimes { get; set; }
    }
}