using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoInput.Models;
using System.Net.Mail;

namespace MongoInput.Providers
{
    public class MongoOldAttachmentProvider
    {
        private IMongoCollection<MongoAttachmentType> AttachTypesCollection;
        private MongoGridFsProvider fsProvider;
        private bool _isSiteEnabled;
        private string _siteId;
        public MongoOldAttachmentProvider(MongoSettings mongoSettings)
        {
            IMongoClient client = new MongoClient(mongoSettings.ConnectionString);
            IMongoDatabase db = client.GetDatabase(mongoSettings.DatabaseName);
            _siteId = mongoSettings.SiteId;
            _isSiteEnabled = mongoSettings.IsSiteEnabled;
            AttachTypesCollection = db.GetCollection<MongoAttachmentType>(GetFullCollectionName("DsAttachmentTypes"));
            fsProvider = new MongoGridFsProvider(mongoSettings);
        }
        public async Task<List<AttachmentEntity>> GetItemAttachmentsAsync(ObjectId relatedItemId, string relatedListId)
        {
            var attachments = await fsProvider.GetAttachmentsByRelatedItemIdAsync(relatedItemId, relatedListId);
            var result = new List<AttachmentEntity>();
            foreach (var a in attachments)
            {
                var content = await fsProvider.GetAttachmentContentByIdAsync(a.Id);
                var entity = await GetAttachmentEntity(a, new MemoryStream(content));

                result.Add(entity);
            }

            return result;
        }

        private async Task<AttachmentEntity> GetAttachmentEntity(GridFSFileInfo fileInfo, Stream content = null)
        {
            var metadata = BsonSerializer.Deserialize<MongoAttachmentMetadata>(fileInfo.Metadata);
            var entity = new AttachmentEntity
            {
                Id = fileInfo.Id.ToString(),
                FileName = fileInfo.Filename,
                RelatedItemId = metadata.RelatedItemId,
                RelatedListId = metadata.RelatedListId,
                Properties = metadata.Properties,
                Content = content,
                UploadDate = fileInfo.UploadDateTime,
                AttachmentType = string.IsNullOrEmpty(metadata.AttachmentTypeId) ? null :
                                    await GetAttachmentTypeByIdAsync(metadata.AttachmentTypeId)
            };

            return entity;
        }

        private async Task<AttachmentTypeEntity> GetAttachmentTypeByIdAsync(string id)
        {
            var attachType = await AttachTypesCollection.Find(Builders<MongoAttachmentType>.Filter.Eq(s => s.Id, id)).FirstOrDefaultAsync();
            return GetEntity(attachType);
        }

        private AttachmentTypeEntity GetEntity(MongoAttachmentType mongoAttachType)
        {
            return mongoAttachType == null ? null : new AttachmentTypeEntity()
            {
                Id = mongoAttachType.Id,
                Title = mongoAttachType.Title,
                ContentTypeId = mongoAttachType.ContentTypeId,
                StaticName = mongoAttachType.StaticName,
                IsDefault = mongoAttachType.IsDefault,
                NotAllowedSelectOnForm = mongoAttachType.NotAllowedSelectOnForm
            };
        }

        private string GetFullCollectionName(string collectionName)
        {
            if (_isSiteEnabled)
            {
                return $"{_siteId}.{collectionName}";
            }
            else
            {
                return collectionName;
            }
        }
    }
}
