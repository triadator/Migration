using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Output
{
    public static class MetadataCollectionExtensions
    {
        private const string MetaDataHeaderPrefix = "x-amz-meta-";

        /// <summary>
        /// Преобразует коллекцию метаданных в словарь ключ-значение, убирая специфические для S3 префиксы.
        /// </summary>
        /// <param name="metadataCollection">Коллекция метаданных S3.</param>
        /// <returns>Словарь, содержащий "чистые" ключи и значения метаданных.</returns>
        public static Dictionary<string, string> ToDictionary(this MetadataCollection metadataCollection)
        {
            return metadataCollection.Keys
                .ToDictionary(
                    key => key.StartsWith(MetaDataHeaderPrefix, StringComparison.OrdinalIgnoreCase)
                           ? key.Substring(MetaDataHeaderPrefix.Length)
                           : key,
                    key => metadataCollection[key]);
        }
    }
}
