using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _userCollection;

        public UserService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var Client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var Database = Client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _userCollection = Database.GetCollection<User>(mongoDBSettings.Value.UserCollectionName);
        }

        public async Task CreateUserAsync(User user)
        {
            await _userCollection.InsertOneAsync(user);
            return;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserById(string id)
        {
            return await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> UpdateUser(string id, User newUser)
        {
            return await _userCollection.FindOneAndReplaceAsync(u => u.Id == id, newUser);
        }

        public async Task DeleteUserAsync(string id)
        { 
            FilterDefinition<User> filter = Builders<User>.Filter.Eq(u => u.Id, id);
            await _userCollection.DeleteOneAsync(filter);
            return;
        }

    }
}
