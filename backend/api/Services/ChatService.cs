using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Services
{
    public class ChatService
    {
        private readonly IMongoCollection<UnReadedMessages> _UnReadedMessageCollection;
        private readonly IMongoCollection<Message> _messageCollection;
        private readonly IMongoCollection<User> _userCollection;

        public ChatService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var Client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var Database = Client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _UnReadedMessageCollection = Database.GetCollection<UnReadedMessages>(mongoDBSettings.Value.UnReadedMessageCollection);
            _messageCollection = Database.GetCollection<Message>(mongoDBSettings.Value.MessageCollection);
            _userCollection = Database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        }

        public async Task SendMessageAsync(Message message,string senderId,string receiverId)
        {
            // Implementation for sending message
            await _messageCollection.InsertOneAsync(message);

            await SetUpdatedUnreadedMessageBetweenUsers(senderId, receiverId);
            return;
        }

        public async Task SetUpdatedUnreadedMessageBetweenUsers(string senderId, string receiverId)
        {
            var filter = Builders<UnReadedMessages>.Filter.And(
                Builders<UnReadedMessages>.Filter.Eq(s=>s.MainUserId, senderId),
                Builders<UnReadedMessages>.Filter.Eq(s => s.OtherUserId, receiverId)
            );

            var update = Builders<UnReadedMessages>.Update
                .Set(r => r.IsReaded, false)
                .Inc(r => r.NumOfUnReadedMessages, 1);

            var options = new FindOneAndUpdateOptions<UnReadedMessages>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            await _UnReadedMessageCollection.FindOneAndUpdateAsync(filter, update, options);
        }

        public async Task<List<Message>> GetMessagesAsync(int page, string user1Id, string user2Id)
        {
            var senderFilter1=Builders<Message>.Filter.Eq(u1=>u1.SenderId, user1Id);
            var receiverFilter1 = Builders<Message>.Filter.Eq(u2 => u2.ReceiverId, user2Id);

            var senderFilter2 = Builders<Message>.Filter.Eq(u1 => u1.ReceiverId, user1Id);
            var receiverFilter2 = Builders<Message>.Filter.Eq(u2 => u2.SenderId, user2Id);

            var combinedFilter=Builders<Message>.Filter.Or
                (
                    Builders<Message>.Filter.And(senderFilter1,receiverFilter1),
                    Builders<Message>.Filter.And(senderFilter2,receiverFilter2)
                );
            
            var sort =Builders<Message>.Sort.Descending(m => m.Id);

            int NumberOfReterningMessages = 8;
            var messages= await _messageCollection
                .Find(combinedFilter)
                .Sort(sort)
                .Skip(page * NumberOfReterningMessages)
                .Limit(NumberOfReterningMessages)
                .ToListAsync();
            
            messages.Reverse();

            return messages;
        }

        public async Task<List<UnReadedMessages>> GetUserUnReadedMessagesAsync(string userId)
        {
            var userFilter = Builders<UnReadedMessages>.Filter.And
                (
                    Builders<UnReadedMessages>.Filter.Eq(u1 => u1.MainUserId, userId),
                    Builders<UnReadedMessages>.Filter.Eq(u2 => u2.IsReaded, false)
                );

            var sort = Builders<UnReadedMessages>.Sort.Descending(m => m.Id);

            var unReadedMessages = await _UnReadedMessageCollection
                .Find(userFilter)
                .Sort(sort)
                .ToListAsync();

            unReadedMessages.Reverse();

            return unReadedMessages;
        }

        public async Task UserReadTheMessge(string unReadedMessagesId)
        {
            var filter = Builders<UnReadedMessages>.Filter.Eq(u1 => u1.Id, unReadedMessagesId);

            var update = Builders<UnReadedMessages>.Update
                .Set(r => r.IsReaded, true)
                .Set(r => r.NumOfUnReadedMessages, 0);

            await _UnReadedMessageCollection.FindOneAndUpdateAsync(filter, update);
        }
    }
}
