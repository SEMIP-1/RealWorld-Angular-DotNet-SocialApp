using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; }

        [BsonElement("details")]
        public string? detaails { get; set; }

        [BsonElement("mainUserId")]
        public string? mainUserId { get; set; }

        [BsonElement("targetId")]
        public string? targetId { get; set; }

        public bool isRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class userIn
    {
        public string name { get; set; }
        public string avatar { get; set; }
    }
}
