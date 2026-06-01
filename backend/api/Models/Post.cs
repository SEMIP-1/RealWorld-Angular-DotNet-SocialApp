using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace api.Models
{
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? _id { get; set; }

        [BsonElement("title")]
        public string? title { get; set; }

        [BsonElement ("creator")]
        public string? creator { get; set; }

        [BsonElement("message")]    
        public string? message { get; set; }

        public string? slectedFiles { get; set; }
        public List<string> likes { get; set; }=new List<string>();
        public List<string> comments { get; set; } = new List<string>();
        public DateTime createdAt { get; set; } = DateTime.Now;
    }
}
