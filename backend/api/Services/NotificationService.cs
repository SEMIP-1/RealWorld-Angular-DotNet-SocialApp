using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace api.Services
{
    public class NotificationService
    {
        private readonly IMongoCollection<Notification> _NotificationCollection;

        public NotificationService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var Client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var Database = Client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _NotificationCollection=Database.GetCollection<Notification>(mongoDBSettings.Value.NotificationCollection);
        }

        public async Task CreateNotification(Notification notification) 
        {
            await _NotificationCollection.InsertOneAsync(notification);

            //TODO Call RealTime Notification grpc


            return;
        }

        public async Task<List<Notification>> GetUserNotification(string userId)
        {
            var filter = Builders<Notification>.Filter.Eq(u => u.mainUserId, userId);
            var userNotification=await _NotificationCollection.Find(filter).SortByDescending(p=>p.CreatedAt).ToListAsync();
            return userNotification;
        }

        public async Task<bool> MarkNotificationAsRead(string userId)
        {
            var filter = Builders<Notification>.Filter.Eq(u => u.mainUserId, userId);
            var update = Builders<Notification>.Update.Set(n => n.isRead, true);

            var result = await _NotificationCollection.UpdateManyAsync(filter, update);
            if (result is null) {return false;}else { return true; }
        }
    }
}