using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using MongoInput;
using MongoInput.Models;
using MongoInput.Providers;
using S3Output;
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

            MongoSettings? mongoSettings = configuration.GetSection("MainAppDatabaseSettings").Get<MongoSettings>();
            MongoOldAttachmentProvider attachmentProvider = new MongoOldAttachmentProvider(mongoSettings);
            MongoListProvider listProvider = new MongoListProvider(mongoSettings);
            MongoNewAttachmentProvider newAttachmentProvider = new MongoNewAttachmentProvider(mongoSettings);

            S3Configuration? s3Settings = configuration.GetSection("S3Settings").Get<S3Configuration>();
            S3Repository s3Repository = new S3Repository(s3Settings);

            var mongoLists = await listProvider.GetAllListsAsync();

            foreach (var (mongo, index) in mongoLists.Select((value, i) => (value, i)))
            {
                
                int counter = 0;
                int pageNumber = 1;
                int pageSize = 40;
                bool hasMoreData = true;

                switch (mongo.ListType)
                {
                    case 100:
                        Console.WriteLine($"Миграция списка: {mongo.ListId} началась.");
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
                                ));
                            }

                                await Task.WhenAll(uploadTasks);
                            }

                            pageNumber++; 
                        }

                        break;

                    case 101:
                        Console.WriteLine($"Миграция библиотеки файлов:{mongo.ListId} началась.");
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
                                    var request = new TransferUtilityUploadRequest
                                    {
                                        BucketName = "files",
                                        Key = $"{mongoSettings.SiteId}/{mongo.ListId}/{element.Id}/{element.FileName}",
                                        InputStream = new MemoryStream(element.FileContent),
                                        AutoCloseStream = true
                                    };

                                    var result = await s3Repository.UploadFileAsync(request);
                                }));
                            }
                            await Task.WhenAll(uploadTasks);
                            pageNumber++;
                        }
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"Смигрировано {counter} элементов.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Осталось коллекций для минграции: {mongoLists.Count - (index + 1)}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine($"Скрипт запокончил работу");
            Console.ReadKey();





        }

    }
}
