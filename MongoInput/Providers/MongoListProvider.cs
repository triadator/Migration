using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoInput.Models;
using System.Collections;

namespace MongoInput.Providers
{
    public class MongoListProvider
    {
        // Создание экземпляра MongoDBConnector
        private IMongoDatabase _db;
        private string _siteId;
        private IMongoCollection<MongoList> _listCollection;
        private bool _isSiteEnabled;
        // Ваш код работы с MongoDB
        // Пример: Получение коллекции и вывод ее имени
        public MongoListProvider(MongoSettings mongoSettings)
        {
            IMongoClient client = new MongoClient(mongoSettings.ConnectionString);
            _db = client.GetDatabase(mongoSettings.DatabaseName);
            _siteId = mongoSettings.SiteId;
            _isSiteEnabled = mongoSettings.IsSiteEnabled;
            _listCollection = _db.GetCollection<MongoList>(GetFullCollectionName("DsLists"));
        }

        public async Task<List<MongoList>> GetAllListsAsync()
        {

            var listFilter = Builders<MongoList>.Filter.And(
                Builders<MongoList>.Filter.Eq(l => l.State, 0)
               , Builders<MongoList>.Filter.Eq(it => it.SystemList, false)
               , Builders<MongoList>.Filter.In(it => it.ListType, new[] { 100, 101 })
                );

            try
            {
                var lists = await _listCollection.Find(listFilter).ToListAsync();
                if (lists.Count == 0) throw new Exception("Empty list collection exception");
                return lists;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

        }

        public async Task<List<ObjectId>> GetCollectionElementsIdsAsync(string collectionName, int pageNumber, int pageSize)
        {
            var collection = _db.GetCollection<MongoElement>(GetFullCollectionName(collectionName));

            var filter = Builders<MongoElement>.Filter.Empty; // Пустой фильтр для получения всех документов
            var result = await collection.Find(filter)
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Limit(pageSize)
                                         .ToListAsync();

            return result.Select(x => x.Id).ToList();
        }

        public async Task<List<MongoListItem>> GetCollectionShortFilesAsync(string collectionName, int pageNumber, int pageSize)
        {
            var collection = _db.GetCollection<MongoListItem>(GetFullCollectionName(collectionName));
            var filter = Builders<MongoListItem>.Filter.Empty; // Пустой фильтр для получения всех документов

            var result = await collection.Find(filter)
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Limit(pageSize)
                                         .ToListAsync();

            return result;
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
