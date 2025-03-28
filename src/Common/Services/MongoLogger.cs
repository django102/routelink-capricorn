using Common.Interfaces;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class MongoLogger : IMongoLogger
    {
        private readonly IMongoCollection<LogEntry> _logs;

        public MongoLogger(IMongoDatabase database, string collectionName = "logs")
        {
            _logs = database.GetCollection<LogEntry>(collectionName);
        }

        public async Task LogInformation(string message, string serviceName)
        {
            await _logs.InsertOneAsync(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Message = message,
                ServiceName = serviceName
            });
        }

        public async Task LogError(string message, Exception ex, string serviceName)
        {
            await _logs.InsertOneAsync(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = "Error",
                Message = $"{message}: {ex.Message}",
                Exception = ex.ToString(),
                ServiceName = serviceName
            });
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string ServiceName { get; set; }
    }
}