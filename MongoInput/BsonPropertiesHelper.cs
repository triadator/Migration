using Microsoft.VisualBasic;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput
{
    public static class BsonPropertiesHelper
    {
        /// <summary>
        /// Получение имени поля для БД из поля класса
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string? GetPropertyDbName<T>(string propertyName)
        {
            return GetPropertyDbName(typeof(T).GetProperty(propertyName));
        }

        /// <summary>Получение имени поля для БД из <see cref="PropertyInfo"/></summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static string? GetPropertyDbName(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return null;

            if (propertyInfo.GetCustomAttribute<BsonIgnoreAttribute>() != null)
                return null;

            if (propertyInfo.GetCustomAttribute(typeof(BsonIdAttribute)) != null)
                return "_id";

            return propertyInfo.GetCustomAttribute<BsonElementAttribute>()?.ElementName ?? propertyInfo.Name;
        }

        /// <summary>
        /// Получение доступных полей для записи в БД из <see cref="Type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<string?> GetAvailableDbProperties(this Type type)
        {
            return type.GetProperties().Select(prop => GetPropertyDbName(prop)).Where(name => !string.IsNullOrEmpty(name));
        }
    }
}
