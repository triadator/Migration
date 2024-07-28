using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoInput.Models;

namespace MongoInput.Providers
{
    public class MongoGridFsProvider
    {
        GridFSBucket _filesBucket;
        public MongoGridFsProvider(MongoSettings mongoSettings)
        {
            IMongoClient client = new MongoClient(mongoSettings.ConnectionString);
            IMongoDatabase db = client.GetDatabase(mongoSettings.DatabaseName);
            _filesBucket = new GridFSBucket(db);
        }

        public async Task<GridFSFileInfo[]> GetAttachmentsByRelatedItemIdAsync(ObjectId relatedItemId, string relatedListId)
        {
            var fieldRelatedItemId = BsonPropertiesHelper.GetPropertyDbName<MongoAttachmentMetadata>(nameof(MongoAttachmentMetadata.RelatedItemId));
            var fieldRelatedListId = BsonPropertiesHelper.GetPropertyDbName<MongoAttachmentMetadata>(nameof(MongoAttachmentMetadata.RelatedListId));

            var files = await _filesBucket.Find(Builders<GridFSFileInfo>.Filter.And(
                Builders<GridFSFileInfo>.Filter.Eq(it => it.Metadata[fieldRelatedItemId], relatedItemId),
                Builders<GridFSFileInfo>.Filter.Eq(it => it.Metadata[fieldRelatedListId], relatedListId)
            )).ToListAsync();

            return files.ToArray();
        }
        //public async Task<GridFSFileInfo[]> GetAttachmentsByRelatedItemIdsAsync(string relatedListId, params ObjectId[] relatedItemIds)
        //{
        //    var fieldRelatedItemId = BsonPropertiesHelper.GetPropertyDbName<MongoAttachmentMetadata>(nameof(MongoAttachmentMetadata.RelatedItemId));
        //    var fieldRelatedListId = BsonPropertiesHelper.GetPropertyDbName<MongoAttachmentMetadata>(nameof(MongoAttachmentMetadata.RelatedListId));

        //    var filter = Builders<GridFSFileInfo>.Filter.And(
        //        Builders<GridFSFileInfo>.Filter.In($"Metadata.{fieldRelatedItemId}", relatedItemIds),
        //        Builders<GridFSFileInfo>.Filter.Eq(it => it.Metadata[fieldRelatedListId], relatedListId)
        //    );

        //    var files = await _filesBucket.Find(filter).ToListAsync();

        //    return files.ToArray();
        //}
        public async Task<byte[]> GetAttachmentContentByIdAsync(ObjectId attachmentId)
        {
            return await _filesBucket.DownloadAsBytesAsync(attachmentId);
        }

    }
}
