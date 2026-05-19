namespace api.Models
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }= null!;
        public string DatabaseName { get; set; }= null!;
        public string UserCollectionName { get; set; }= null!;

    }
}
