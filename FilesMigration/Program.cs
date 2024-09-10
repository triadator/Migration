using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoInput;
using MongoInput.Models;
using MongoInput.Providers;
using S3Output;
using Serilog;

namespace FilesMigration
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .WriteTo.Console()
                            .WriteTo.File(
                                            path: "logs/log.txt",
                                            retainedFileCountLimit: 1,
                                            rollOnFileSizeLimit: true
                            ).CreateLogger();

            Log.Information("Скрипт начал работу");

            List<MongoList> mongoLists = null;
            MongoSettings mongoSettings = null;
            MongoOldAttachmentProvider attachmentProvider = null;
            MongoListProvider listProvider = null;
            MongoNewAttachmentProvider newAttachmentProvider = null;
            S3Configuration ? s3Settings = null;
            S3Repository s3Repository = null;
           
            // Получаем конфиги и сервисы.
            try
            {
                mongoSettings = configuration.GetSection("MainAppDatabaseSettings").Get<MongoSettings>();
                attachmentProvider = new MongoOldAttachmentProvider(mongoSettings);
                listProvider = new MongoListProvider(mongoSettings);
                newAttachmentProvider = new MongoNewAttachmentProvider(mongoSettings);

                s3Settings = configuration.GetSection("S3Settings").Get<S3Configuration>();
                s3Repository = new S3Repository(s3Settings);


                mongoLists = await listProvider.GetAllListsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            // Если в конфиге есть настройка на миграцию конкретных отдельных коллекций базы, то делаем их.
            if (mongoSettings.CollectionNames is not null && mongoSettings.CollectionNames.Any())
            {
                mongoLists = mongoLists.Where(x => mongoSettings.CollectionNames.Contains(x.ListId)).ToList();
            }

            var unhandledCollections = new HashSet<string>();

            // Основная логика. Перебираем коллекции базы
            foreach (var (mongo, index) in mongoLists.Select((value, i) => (value, i)))
            {
                try
                {
                    int counter = 0;
                    ObjectId? lastElementId = null;
                    string lastElementStringId = null;
                    int pageSize = 40;
                    bool hasMoreData = true;
                    
                    //В зависимости от типа коллекции мигрируем ее определенным способом.
                    switch (mongo.ListType)
                    {
                        case 100:
                            Log.Information($"Миграция списка: {mongo.ListId} началась.");
                            while (hasMoreData)
                            {
                                var elements = await listProvider.GetCollectionElementsIdsAsync(mongo.ListId, lastElementId, pageSize);

                                if (elements.Count < pageSize)
                                {
                                    hasMoreData = false;
                                }

                                if (elements.Any())
                                {
                                    lastElementId = elements.Last(); 
                                }

                                // Обработка каждого элемента
                                var uploadTasks = elements.Select(async element =>
                                {
                                    try
                                    {
                                        var attachmentEntities = await attachmentProvider.GetItemAttachmentsAsync(element, mongo.ListId);
                                        counter += attachmentEntities.Count;

                                        // Загрузка вложений
                                        var uploadAttachmentTasks = attachmentEntities.Select(async e =>
                                        {
                                            try
                                            {
                                                var request = new TransferUtilityUploadRequest
                                                {
                                                    BucketName = "attachments",
                                                    Key = $"{mongoSettings.SiteId}/{e.RelatedListId}/{e.RelatedItemId}/{e.FileName}",
                                                    InputStream = e.Content,
                                                    //Автозакрытие используемого потоа после загрузки.
                                                    AutoCloseStream = true
                                                };

                                                var attachment = new MongoAttachment
                                                {
                                                    FileName = e.FileName,
                                                    FileSize = e.Content.Length,
                                                    SiteId = mongoSettings.SiteId,
                                                    RelatedListId = e.RelatedListId,
                                                    RelatedItemId = e.RelatedItemId,
                                                    AttachmentTypeId = e.AttachmentType?.Id,
                                                    CreatedBy = e.Properties?["CreatedBy"]?.ToString(),
                                                    ModifiedBy = e.Properties?["ModifiedBy"]?.ToString(),
                                                };

                                                //S3 API не поддерживает загрузки пачками, только по 1 файлу.
                                                var result = await s3Repository.UploadFileAsync(request);

                                                attachment.Updated = result.LastModified;
/// (!!!) Если необходимо протестировать скорость, не загружая элементы в монго, то эту строку нужно закомментировать.
/// После теста скорости бакет в минаё можно очистить, либо вообще ничего не делать с минаё, тогда данные перезапишутся.

                                                await newAttachmentProvider.InsertOne(attachment);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error($"Ошибка при загрузке вложения: {e.FileName}, RelatedItem ID: {e.RelatedItemId}, FileName: {e.FileName}, Exception: {ex.Message}");
                                                unhandledCollections.Add(mongo.ListId);
                                            }
                                        });

                                        await Task.WhenAll(uploadAttachmentTasks);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"Ошибка при обработке элемента: {element}, Exception: {ex.Message}");
                                    }
                                });

                                // Запуск всех задач параллельно
                                await Task.WhenAll(uploadTasks);

                            }
                            break;

                        case 101:
                            Log.Information($"Миграция библиотеки файлов: {mongo.ListId} началась.");
                            while (hasMoreData)
                            {
                                var files = await listProvider.GetCollectionShortFilesAsync(mongo.ListId, lastElementStringId, pageSize);

                                if (files.Count < pageSize)
                                {
                                    hasMoreData = false;
                                }

                                if (files.Any())
                                {
                                    lastElementStringId = files.LastOrDefault()?.Id; // Обновляем идентификатор последнего элемента
                                }

                                //Файлы в монго грузить не надо, онитам и так есть. В case 101 мы просто выгружаем данные из поля FileContent в S3.
                                var uploadTasks = files
                                    .Where(e => e.FileContent != null && e.FileName != null)
                                    .Select(async element =>
                                    {
                                        try
                                        {
                                            counter++;
                                            var request = new TransferUtilityUploadRequest
                                            {
                                                BucketName = "files",
                                                Key = $"{mongoSettings.SiteId}/{mongo.ListId}/{element.Id}/{element.FileName}",
                                                InputStream = new MemoryStream(element.FileContent),
                                                AutoCloseStream = true
                                            };

                                            await s3Repository.UploadFileAsync(request);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Information($"Ошибка при загрузке файла: {element.FileName}, Item ID: {element.Id}, FileName: {element.FileName}, Exception: {ex.Message}");
                                            unhandledCollections.Add(mongo.ListId);
                                        }
                                    });

                                await Task.WhenAll(uploadTasks);

                            }
                            break;

                        default:
                            break;
                    }

                    Log.Information($"Смигрировано {counter} элементов.");
                    Log.Information($"Осталось коллекций для миграции: {mongoLists.Count - (index + 1)}");
                }
                catch (Exception ex)
                {
                    Log.Information($"Ошибка при миграции: {mongo.ListId}, Exception: {ex.Message}");
                }
            }

            if (!unhandledCollections.Any())
            {
                Log.Information($"Все коллекции обработаны успешно");
            }
            else
            {
                string unhandledCollectionsList = string.Join(", ", unhandledCollections);
                Log.Information($"Коллекции, обработанные с ошибкой: {unhandledCollectionsList}");
            }
            Log.Information($"Скрипт закончил работу");
        }

    }
}
