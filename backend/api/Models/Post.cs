using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace api.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? id { get; set; } =null!;

        [BsonElement("title")]
        public string? title { get; set; }

        [BsonElement ("creator")]
        public string? creator { get; set; } = null!;

        [BsonElement("message")]    
        public string? message { get; set; }

        public string? selectedFiles { get; set; }
        public HashSet<string> likes { get; set; }=new HashSet<string>();
        public HashSet<string> comments { get; set; } = new HashSet<string>();
        public DateTime createdAt { get; set; } = DateTime.Now;
    }
}
