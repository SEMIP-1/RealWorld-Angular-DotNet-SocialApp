using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace api.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}
