using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput.Models
{
    public class MongoAttachmentType
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ContentTypeId { get; set; }
        public string StaticName { get; set; }
        public bool IsDefault { get; set; }
        public bool NotAllowedSelectOnForm { get; set; }
    }
}
