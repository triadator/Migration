using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MongoInput.Models
{
    [BsonIgnoreExtraElements]
    public class MongoElement
    {
        [BsonId]
        public ObjectId Id { get; set; }
    }
}
