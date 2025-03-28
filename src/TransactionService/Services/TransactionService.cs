using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Polly;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using TransactionService.Interfaces;
using TransactionService.Models;
using TransactionService.Mappings;

namespace TransactionService.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICapricornService _capricornService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;
        private readonly IFraudDetectionService _fraudDetectionService;

        public TransactionService(
            ITransactionRepository transactionRepository,
            ICapricornService capricornService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<TransactionService> logger,
            IFraudDetectionService fraudDetectionService)
        {
            _transactionRepository = transactionRepository;
            _capricornService = capricornService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
            _fraudDetectionService = fraudDetectionService;
        }

        public async Task<Transaction> PurchaseAirtime(AirtimePurchaseRequest request, string userId)
        {
            // Validate idempotency
            if (await _transactionRepository.IsDuplicateTransaction(request.IdempotencyKey))
            {
                _logger.LogWarning("Duplicate transaction attempt detected for idempotency key: {IdempotencyKey}", request.IdempotencyKey);
                throw new InvalidOperationException("Duplicate transaction detected");
            }

            // Fraud detection
            var fraudRequest = new TransactionEvaluationRequest
            {
                UserId = userId,
                TransactionType = "Airtime",
                Amount = request.Amount,
                Recipient = request.PhoneNumber
            };

            var fraudResult = await _fraudDetectionService.EvaluateTransaction(fraudRequest);
            if (fraudResult.IsFraudulent)
            {
                _logger.LogWarning("Fraud detected for transaction: {FraudScore} - {Reason}",
                    fraudResult.FraudScore, fraudResult.Reason);

                transaction.Status = "Blocked";
                transaction.FailureReason = $"Fraud detected: {fraudResult.Reason}";
                await _transactionRepository.UpdateTransaction(transaction);

                throw new InvalidOperationException("Transaction blocked due to fraud detection");
            }

            // Create transaction record
            var transaction = new Transaction
            {
                UserId = userId,
                TransactionType = "Airtime",
                Provider = request.Provider,
                Recipient = request.PhoneNumber,
                Amount = request.Amount,
                Status = "Pending",
                IdempotencyKey = request.IdempotencyKey ?? GenerateIdempotencyKey()
            };

            // Save initial transaction state
            transaction = await _transactionRepository.AddTransaction(transaction);

            try
            {
                // Process with Capricorn
                var result = await _capricornService.PurchaseAirtime(request);

                // Update transaction status
                transaction.Status = "Completed";
                transaction.Reference = result.Reference;
                await _transactionRepository.UpdateTransaction(transaction);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing airtime purchase for transaction {TransactionId}", transaction.Id);

                // Update transaction status
                transaction.Status = "Failed";
                transaction.FailureReason = ex.Message;
                await _transactionRepository.UpdateTransaction(transaction);

                throw;
            }
        }

        public async Task<Transaction> PurchaseData(DataPurchaseRequest request, string userId)
        {
            // Validate idempotency
            if (await _transactionRepository.IsDuplicateTransaction(request.IdempotencyKey))
            {
                _logger.LogWarning("Duplicate data purchase attempt detected for idempotency key: {IdempotencyKey}",
                    request.IdempotencyKey);
                throw new InvalidOperationException("Duplicate transaction detected");
            }

            // Fraud detection
            var fraudRequest = new TransactionEvaluationRequest
            {
                UserId = userId,
                TransactionType = "Airtime",
                Amount = request.Amount,
                Recipient = request.PhoneNumber
            };

            var fraudResult = await _fraudDetectionService.EvaluateTransaction(fraudRequest);
            if (fraudResult.IsFraudulent)
            {
                _logger.LogWarning("Fraud detected for transaction: {FraudScore} - {Reason}",
                    fraudResult.FraudScore, fraudResult.Reason);

                transaction.Status = "Blocked";
                transaction.FailureReason = $"Fraud detected: {fraudResult.Reason}";
                await _transactionRepository.UpdateTransaction(transaction);

                throw new InvalidOperationException("Transaction blocked due to fraud detection");
            }

            // Create transaction record
            var transaction = new Transaction
            {
                UserId = userId,
                TransactionType = "Data",
                Provider = request.Provider,
                Recipient = request.PhoneNumber,
                Amount = 0, // Will be set by provider based on data plan
                Status = "Pending",
                IdempotencyKey = request.IdempotencyKey ?? GenerateIdempotencyKey()
            };

            // Save initial transaction state
            transaction = await _transactionRepository.AddTransaction(transaction);

            try
            {
                // Process with Capricorn
                var result = await _capricornService.PurchaseData(request);

                // Update transaction with details from provider
                transaction.Status = "Completed";
                transaction.Reference = result.Reference;
                transaction.Amount = result.Amount;
                await _transactionRepository.UpdateTransaction(transaction);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data purchase for transaction {TransactionId}", transaction.Id);

                // Update transaction status
                transaction.Status = "Failed";
                transaction.FailureReason = ex.Message;
                await _transactionRepository.UpdateTransaction(transaction);

                throw;
            }
        }

        public async Task<Transaction> SubscribeTv(TvSubscriptionRequest request, string userId)
        {
            // Validate idempotency
            if (await _transactionRepository.IsDuplicateTransaction(request.IdempotencyKey))
            {
                _logger.LogWarning("Duplicate TV subscription attempt detected for idempotency key: {IdempotencyKey}",
                    request.IdempotencyKey);
                throw new InvalidOperationException("Duplicate transaction detected");
            }

            // Fraud detection
            var fraudRequest = new TransactionEvaluationRequest
            {
                UserId = userId,
                TransactionType = "Airtime",
                Amount = request.Amount,
                Recipient = request.PhoneNumber
            };

            var fraudResult = await _fraudDetectionService.EvaluateTransaction(fraudRequest);
            if (fraudResult.IsFraudulent)
            {
                _logger.LogWarning("Fraud detected for transaction: {FraudScore} - {Reason}",
                    fraudResult.FraudScore, fraudResult.Reason);

                transaction.Status = "Blocked";
                transaction.FailureReason = $"Fraud detected: {fraudResult.Reason}";
                await _transactionRepository.UpdateTransaction(transaction);

                throw new InvalidOperationException("Transaction blocked due to fraud detection");
            }

            // Create transaction record
            var transaction = new Transaction
            {
                UserId = userId,
                TransactionType = "TV",
                Provider = request.Provider,
                Recipient = request.SmartCardNumber,
                Amount = 0, // Will be set by provider based on subscription plan
                Status = "Pending",
                IdempotencyKey = request.IdempotencyKey ?? GenerateIdempotencyKey()
            };

            // Save initial transaction state
            transaction = await _transactionRepository.AddTransaction(transaction);

            try
            {
                // Process with Capricorn
                var result = await _capricornService.SubscribeTv(request);

                // Update transaction with details from provider
                transaction.Status = "Completed";
                transaction.Reference = result.Reference;
                transaction.Amount = result.Amount;
                await _transactionRepository.UpdateTransaction(transaction);

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TV subscription for transaction {TransactionId}", transaction.Id);

                // Update transaction status
                transaction.Status = "Failed";
                transaction.FailureReason = ex.Message;
                await _transactionRepository.UpdateTransaction(transaction);

                throw;
            }
        }

        public async Task<Transaction> GetTransactionById(Guid id, string userId)
        {
            var transaction = await _transactionRepository.GetTransactionById(id);

            if (transaction == null || transaction.UserId != userId)
            {
                throw new KeyNotFoundException("Transaction not found or access denied");
            }

            return transaction;
        }

        private string GenerateIdempotencyKey()
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}