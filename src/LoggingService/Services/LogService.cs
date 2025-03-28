using LoggingService.Models;
using MongoDB.Driver;

namespace LoggingService.Services
{
    public class LogService
    {
        private readonly IMongoCollection<ApiLog> _logs;

        public LogService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDbSettings:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDbSettings:DatabaseName"]);
            _logs = database.GetCollection<ApiLog>("ApiLogs");
        }

        public async Task LogRequest(ApiLog log)
        {
            await _logs.InsertOneAsync(log);
        }

        public async Task<IEnumerable<ApiLog>> GetLogs(DateTime from, DateTime to)
        {
            var filter = Builders<ApiLog>.Filter.And(
                Builders<ApiLog>.Filter.Gte(x => x.Timestamp, from),
                Builders<ApiLog>.Filter.Lte(x => x.Timestamp, to)
            );
            
            return await _logs.Find(filter).SortByDescending(x => x.Timestamp).ToListAsync();
        }
    }
}