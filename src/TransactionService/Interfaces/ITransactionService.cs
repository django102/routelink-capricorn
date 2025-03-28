using TransactionService.Models;

namespace TransactionService.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> PurchaseAirtime(AirtimePurchaseRequest request, string userId);
        Task<Transaction> PurchaseData(DataPurchaseRequest request, string userId);
        Task<Transaction> SubscribeTv(TvSubscriptionRequest request, string userId);
        Task<Transaction> GetTransactionById(Guid id, string userId);
    }

    public interface ICapricornService
    {
        Task<Transaction> PurchaseAirtime(AirtimePurchaseRequest request);
        Task<Transaction> PurchaseData(DataPurchaseRequest request);
        Task<Transaction> SubscribeTv(TvSubscriptionRequest request);
    }

    public interface ITransactionRepository
    {
        Task<Transaction> AddTransaction(Transaction transaction);
        Task<Transaction> GetTransactionById(Guid id);
        Task<Transaction> GetTransactionByIdempotencyKey(string idempotencyKey);
        Task<bool> IsDuplicateTransaction(string idempotencyKey);
    }

    public interface ICacheService
    {
        Task SetCacheAsync(string key, object value, TimeSpan? expiry = null);
        Task<T> GetCacheAsync<T>(string key);
        Task<bool> RemoveCacheAsync(string key);
    }
}