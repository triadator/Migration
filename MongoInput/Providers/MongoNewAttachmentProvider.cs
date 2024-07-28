using MongoDB.Bson;
using MongoDB.Driver;
using MongoInput.Models;
using System.Net.Mail;

namespace MongoInput.Providers
{
    public class MongoNewAttachmentProvider
    {
        private IMongoCollection<MongoAttachment> AttachmentsCollection;
        private bool _isSiteEnabled;
        private string _siteId;
        public MongoNewAttachmentProvider(MongoSettings mongoSettings)
        {
            IMongoClient client = new MongoClient(mongoSettings.ConnectionString);
            IMongoDatabase db = client.GetDatabase(mongoSettings.DatabaseName);
            _siteId = mongoSettings.SiteId;
            _isSiteEnabled = mongoSettings.IsSiteEnabled;
            AttachmentsCollection = db.GetCollection<MongoAttachment>(GetFullCollectionName("DsAttachments"));

        }

        public async Task InsertOne(MongoAttachment attachment)
        {
             await AttachmentsCollection.InsertOneAsync(attachment);
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
