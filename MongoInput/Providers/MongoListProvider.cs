using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoInput.Models;
using System.Collections;

namespace MongoInput.Providers
{
    public class MongoListProvider
    {
        private IMongoDatabase _db;
        private string _siteId;
        private IMongoCollection<MongoList> _listCollection;
        private bool _isSiteEnabled;

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

        /// <summary>
        /// Асинхронно получает список ObjectId элементов из коллекции MongoDB с пагинацией через lastObjectId.
        /// Можно также сделать через asyncCursor
        /// </summary>
        /// <param name="collectionName">Название коллекции.</param>
        /// <param name="lastId">Последний ObjectId, с которого начинать выборку (опционально).</param>
        /// <param name="pageSize">Количество элементов на одну страницу.</param>
        /// <returns>Список ObjectId элементов коллекции.</returns>
        public async Task<List<ObjectId>> GetCollectionElementsIdsAsync(string collectionName, ObjectId? lastId, int pageSize)
        {
            var collection = _db.GetCollection<MongoElement>(GetFullCollectionName(collectionName));

            var filter = lastId.HasValue
                ? Builders<MongoElement>.Filter.Gt(x => x.Id, lastId.Value)
                : Builders<MongoElement>.Filter.Empty;

            var result = await collection.Find(filter)
                                         .Limit(pageSize)
                                         .ToListAsync();

            return result.Select(x => x.Id).ToList();
        }

        /// <summary>
        /// Асинхронно получает краткий список файлов (элементов) из коллекции MongoDB с пагинацией.
        /// </summary>
        /// <param name="collectionName">Название коллекции, из которой производится выборка.</param>
        /// <param name="lastId">Идентификатор последнего элемента для пагинации (опционально).</param>
        /// <param name="pageSize">Количество элементов на страницу для пагинации.</param>
        /// <returns>Список элементов <see cref="MongoListItem"/> из коллекции.</returns>
        public async Task<List<MongoListItem>> GetCollectionShortFilesAsync(string collectionName, string? lastId, int pageSize)
        {
            var collection = _db.GetCollection<MongoListItem>(GetFullCollectionName(collectionName));

            var filter = !string.IsNullOrEmpty(lastId)
                ? Builders<MongoListItem>.Filter.Gt(x => x.Id, lastId)
                : Builders<MongoListItem>.Filter.Empty;

            var result = await collection.Find(filter)
                                         .Sort(Builders<MongoListItem>.Sort.Ascending(x => x.Id))
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
