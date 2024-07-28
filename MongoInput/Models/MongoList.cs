using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace MongoInput.Models
{
    public class MongoList
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ListId { get; set; }
        [BsonElement("Name")]
        public string Name { get; set; }
        public int State { get; set; }
        public string Description { get; set; }
        public string ListUrl { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string DefaultView { get; set; }
        public int ListType { get; set; }
        public bool EnableFolderCreation { get; set; }
        public bool SystemList { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public bool VersioningEnabled { get; set; }
        public int VersionLimit { get; set; }
        public string VersionStorage { get; set; }
        public bool ConfidentialityEnabled { get; set; }
        public string SiteId { get; set; }

        public MongoList()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}
