using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface ICacheService
    {
        Task SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T> GetCacheAsync<T>(string key);
        Task<bool> RemoveCacheAsync(string key);
        Task<bool> KeyExistsAsync(string key);
    }
}