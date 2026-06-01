using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace api.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("password")]
        public string Password { get; set; } = null!;

        public string imageUrl { get; set; } = null!;
        public string bio { get; set; } = null!;

        public HashSet<string> followers { get; set; } = new HashSet<string>();
        public HashSet<string> following { get; set; } = new HashSet<string>();

        internal string DecryptPasswordBase64(string base64EncodedData)
        {
            var base64EncodedBytes=System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        internal string EncryptPasswordBase64(string text)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    }
}
