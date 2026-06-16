using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Services
{
    public class MessageService
    {
        public MessageService() 
        {
            
        }
        private readonly IMongoCollection<Message> _messageCollection;
        private readonly IMongoCollection<User> _userCollection;

        public MessageService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var Client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var Database = Client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _messageCollection = Database.GetCollection<Message>(mongoDBSettings.Value.MessageCollection);
            _userCollection = Database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        }
    }
}
