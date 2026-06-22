using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models
{
    public class UnReadedMessages
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }=null!;
        public string MainUserId { get; set; } = null!;
        public string OtherUserId { get; set; } = null!;
        public bool IsReaded { get; set; } = false;
        public int NumOfUnReadedMessages { get; set; } = 0;
    }
}
