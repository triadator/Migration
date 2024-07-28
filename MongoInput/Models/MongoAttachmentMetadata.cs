using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput.Models
{
    [BsonIgnoreExtraElements]
    public class MongoAttachmentMetadata
    {
        [BsonElement("RelatedItemId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RelatedItemId { get; set; }

        [BsonElement("RelatedListId")]
        [BsonRepresentation(BsonType.String)]
        public string RelatedListId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        [BsonIgnoreIfNull]
        public string AttachmentTypeId { get; set; }

        [BsonElement("Properties")]
        public Dictionary<string, object> Properties { get; set; }

        public MongoAttachmentMetadata()
        {
            Properties = new Dictionary<string, object>();
        }
    }
}
