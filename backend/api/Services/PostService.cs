using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace api.Services
{
    public class PostService
    {
        private readonly IMongoCollection<Post> _postCollection;
        private readonly IMongoCollection<User> _userCollection;

        public PostService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var Client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var Database = Client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _postCollection = Database.GetCollection<Post>(mongoDBSettings.Value.PostCollection);
            _userCollection = Database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        }

        public async Task CreateOnePostAsync(Post post)
        {
            await _postCollection.InsertOneAsync(post);
            return;
        }

        public async Task<Post?> UpdatePost(string id, Post newPost)
        {
            return await _postCollection.FindOneAndReplaceAsync(p => p._id == id, newPost);
        }

        public async Task<Post?> GetPostById(string id)
        {
            return await _postCollection.Find(p => p._id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserById(string id)
        {
            return await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeletePostAsync(string id)
        {
            FilterDefinition<Post> filter = Builders<Post>.Filter.Eq(p => p._id, id);
            await _postCollection.DeleteOneAsync(filter);
            return;
        }

        public async Task<(List<Post>, List<User>)> Search(string searchQuery)
        {
            FilterDefinition<Post> FillterPost = new BsonDocument
            {
                {"title",new BsonDocument("$regex",searchQuery)},
                {"message",new BsonDocument("$regex",searchQuery)}
            };

            FilterDefinition<User> FillterUser = new BsonDocument
            {
                {"Username",new BsonDocument("$regex",searchQuery)},
                {"email",new BsonDocument("$regex",searchQuery)}
            };

            List<Post> posts = (await _postCollection.FindAsync(FillterPost)).ToList();
            List<User> users = (await _userCollection.FindAsync(FillterUser)).ToList();

            if (posts is null)
            {
                posts = new List<Post>();
            }
            else if (users is null)
            { 
                users = new List<User>();
            }

            return (posts, users);
        }

        public Object Query(List<string>ides, int? queryPage)
        {
            var filter = Builders<Post>.Filter.Empty;
            foreach(var id in ides)
            {
                filter = Builders<Post>.Filter.Regex("creator", new BsonRegularExpression(id,"i"));
            }

            var sort = Builders<Post>.Sort.Descending("_id");
            var find =_postCollection.Find(filter).Sort(sort);

            int currentPage = queryPage.GetValueOrDefault(1)==0? 1: queryPage.GetValueOrDefault(1);
            int perPage = 4;

            int numberOfPages = find.CountDocuments() /perPage;
            return new
            {
                data = find.Skip((currentPage - 1) * perPage).Limit(perPage).ToList(),
                numberOfPages,
                currentPage,
            };
        }

    }
}