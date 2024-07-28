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
    public class MongoAttachment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

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

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public string CreatedBy { get; set; }

        public string ModifiedBy { get; set; }

        [BsonElement("Updated")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc, Representation = BsonType.DateTime)]
        public DateTime Updated { get; set; }

        public string SiteId { get; set; }
    }
}
