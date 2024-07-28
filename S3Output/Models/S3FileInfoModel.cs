using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Output.Models
{
    public class S3FileInfoModel
    {
        /// <summary>
        /// Уникальный идентификатор файла. Состоит из Path + Name.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// Уникальный идентификатор версии.
        /// </summary>
        public string VersionId { get; init; }

        /// <summary>
        /// Время изменения файла.
        /// </summary>
        public DateTime LastModified { get; init; }

        /// <summary>
        /// Имя бакета.
        /// </summary>
        public string BucketName { get; init; }

        /// <summary>
        /// Размер файла с данными.
        /// </summary>
        public long СontentLength { get; init; }

        /// <summary>
        /// Срок хранения файла.
        /// </summary>
        public DateTime? ExpirationDate { get; init; }

        /// <summary>
        /// Дополнительные метаданные, связанные с файлом.
        /// </summary>
        public IDictionary<string, string>? Metadata { get; init; }
    }
}
