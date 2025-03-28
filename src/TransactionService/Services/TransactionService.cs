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

        public TransactionService(
            ITransactionRepository transactionRepository,
            ICapricornService capricornService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _capricornService = capricornService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Transaction> PurchaseAirtime(AirtimePurchaseRequest request, string userId)
        {
            // Validate idempotency
            if (await _transactionRepository.IsDuplicateTransaction(request.IdempotencyKey))
            {
                _logger.LogWarning("Duplicate transaction attempt detected for idempotency key: {IdempotencyKey}", request.IdempotencyKey);
                throw new InvalidOperationException("Duplicate transaction detected");
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
            // Similar implementation to PurchaseAirtime but for data
            // Omitted for brevity
        }

        public async Task<Transaction> SubscribeTv(TvSubscriptionRequest request, string userId)
        {
            // Similar implementation to PurchaseAirtime but for TV
            // Omitted for brevity
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