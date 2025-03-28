using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoggingService.Models
{
    public class ApiLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ServiceName { get; set; }
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public long DurationMs { get; set; }
    }
}