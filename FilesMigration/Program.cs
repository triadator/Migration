using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using MongoInput;
using MongoInput.Models;
using MongoInput.Providers;
using S3Output;
using Serilog;
using System;

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

            MongoSettings? mongoSettings = configuration.GetSection("MainAppDatabaseSettings").Get<MongoSettings>();
            MongoOldAttachmentProvider attachmentProvider = new MongoOldAttachmentProvider(mongoSettings);
            MongoListProvider listProvider = new MongoListProvider(mongoSettings);
            MongoNewAttachmentProvider newAttachmentProvider = new MongoNewAttachmentProvider(mongoSettings);

            S3Configuration? s3Settings = configuration.GetSection("S3Settings").Get<S3Configuration>();
            S3Repository s3Repository = new S3Repository(s3Settings);


            var mongoLists = await listProvider.GetAllListsAsync();

            if (mongoSettings.CollectionNames is not null && mongoSettings.CollectionNames.Any())
            {
                mongoLists = mongoLists.Where(x => mongoSettings.CollectionNames.Contains(x.ListId)).ToList();
            }

            var unhandledCollections = new HashSet<string>();

            foreach (var (mongo, index) in mongoLists.Select((value, i) => (value, i)))
            {
                try
                {
                    int counter = 0;
                    int pageNumber = 1;
                    int pageSize = 40;
                    bool hasMoreData = true;

                    switch (mongo.ListType)
                    {
                        case 100:
                            Log.Information($"Миграция списка: {mongo.ListId} началась.");
                            while (hasMoreData)
                            {
                                var elements = await listProvider.GetCollectionElementsIdsAsync(mongo.ListId, pageNumber, pageSize);

                                if (elements.Count < pageSize)
                                {
                                    hasMoreData = false;
                                }

                                foreach (var element in elements)
                                {
                                    var attachmentEntities = await attachmentProvider.GetItemAttachmentsAsync(element, mongo.ListId);
                                    counter += attachmentEntities.Count;

                                    // Создаем список задач для загрузки аттачментов
                                    var uploadTasks = new List<Task>();

                                    foreach (var e in attachmentEntities)
                                    {
                                        uploadTasks.Add(Task.Run(async () =>
                                        {
                                            try
                                            {
                                                var request = new TransferUtilityUploadRequest
                                                {
                                                    BucketName = "attachments",
                                                    Key = $"{mongoSettings.SiteId}/{e.RelatedListId}/{e.RelatedItemId}/{e.FileName}",
                                                    InputStream = e.Content,
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

                                                var result = await s3Repository.UploadFileAsync(request);

                                                attachment.Updated = result.LastModified;

                                                await newAttachmentProvider.InsertOne(attachment);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error($"Ошибка при загрузке вложения: {e.FileName}, RelatedItem ID: {e.RelatedItemId}, FileName: {e.FileName}, Exception: {ex.Message}");
                                                unhandledCollections.Add(mongo.ListId);

                                            }
                                        }
                                    ));
                                    }

                                    await Task.WhenAll(uploadTasks);
                                }

                                pageNumber++;
                            }

                            break;

                        case 101:
                            Log.Information($"Миграция библиотеки файлов:{mongo.ListId} началась.");
                            while (hasMoreData)
                            {
                                var files = await listProvider.GetCollectionShortFilesAsync(mongo.ListId, pageNumber, pageSize);

                                if (files.Count < pageSize)
                                {
                                    hasMoreData = false;
                                }
                                var uploadTasks = new List<Task>();

                                foreach (var element in files)
                                {
                                    if (element.FileContent == null || element.FileName == null)
                                    {
                                        continue;
                                    }

                                    counter++;
                                    uploadTasks.Add(Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var request = new TransferUtilityUploadRequest
                                            {
                                                BucketName = "files",
                                                Key = $"{mongoSettings.SiteId}/{mongo.ListId}/{element.Id}/{element.FileName}",
                                                InputStream = new MemoryStream(element.FileContent),
                                                AutoCloseStream = true
                                            };

                                            var result = await s3Repository.UploadFileAsync(request);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Information($"Ошибка при загрузке файла: {element.FileName}, Item ID: {element.Id},FileName: {element.FileName}, Exception: {ex.Message}");
                                            unhandledCollections.Add(mongo.ListId);
                                        }
                                    }));
                                }
                                await Task.WhenAll(uploadTasks);
                                pageNumber++;
                            }
                            break;
                        default:
                            break;
                    }
                    Log.Information($"Смигрировано {counter} элементов.");
                    Log.Information($"Осталось коллекций для миграции: {mongoLists.Count - (index + 1)}");
                }
                catch (Exception)
                {
                    Log.Information($"Ошибка при миграции: {mongo.ListId}");
                    continue;
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
