using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Core
{
    public static class TypeExtensions
    {
        private static readonly IDictionary<Type, PropertyInformation> EntityProperties =
            new ConcurrentDictionary<Type, PropertyInformation>();

        public static PropertyInformation GetPropertyInfo(this Type type)
        {
            if (EntityProperties.TryGetValue(type, out _))
                return EntityProperties[type];

            var properties = type.GetProperties();

            var propertyGetter = properties.ToDictionary(
                property => property.Name,
                property => property.CreateGetter());

            var propertySetter = properties.ToDictionary(
                property => property.Name,
                property => property.CreateSetter());

            EntityProperties[type] = new PropertyInformation(properties, propertyGetter, propertySetter);

            return EntityProperties[type];
        }

        public static Func<object, object> CreateGetter(this PropertyInfo pi)
        {
            if (!pi.CanRead)
            {
                return null;
            }

            if (pi.DeclaringType == null)
            {
                return null;
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var convertP = Expression.TypeAs(instance, pi.DeclaringType);
            var property = Expression.Property(convertP, pi);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<object, object>) Expression.Lambda(convert, instance).Compile();
        }

        public static Action<object, object> CreateSetter(this PropertyInfo pi)
        {
            if (!pi.CanWrite)
            {
                return null;
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var value = Expression.Parameter(typeof(object));
            if (pi.DeclaringType == null)
            {
                return null;
            }

            var convertedParam = Expression.Convert(instance, pi.DeclaringType);
            var propExp = Expression.Property(convertedParam, pi.Name);
            var assignExp = Expression.Assign(propExp, Expression.Convert(value, pi.PropertyType));
            return Expression.Lambda<Action<object, object>>(assignExp, instance, value).Compile();
        }

        public static FormUrlEncodedContent ToFormRequest(this object request)
        {
            if (request == null)
                return null;

            // Get all properties on the object

            var propInfo = request.GetType().GetPropertyInfo();

            var properties = propInfo.Properties
                .Where(x => x.CanRead)
                .Where(x => propInfo.ValueGetterByName[x.Name](request) != null)
                .ToDictionary(x => x.Name, x => propInfo.ValueGetterByName[x.Name](request));

            // Get names for all IEnumerable properties (excl. string)
            var propertyNames = properties
                .Where(x => !(x.Value is string) && x.Value is IEnumerable)
                .Select(x => x.Key)
                .ToList();

            // Concat all IEnumerable properties into a comma separated string
            foreach (var key in propertyNames)
            {
                var valueType = properties[key]?.GetType();
                if (valueType == null)
                    continue;

                var valueElemType = valueType.IsGenericType
                    ? valueType.GetGenericArguments()[0]
                    : valueType.GetElementType();
                if (valueElemType == null)
                    continue;

                if (valueElemType.IsPrimitive || valueElemType == typeof(string))
                {
                    var enumerable = properties[key] as IEnumerable;
                    properties[key] = string.Join(",",
                        (enumerable ?? throw new InvalidOperationException()).Cast<object>());
                }
            }

            // Concat all key/value pairs into a string separated by ampersand

            var dic = properties
                .ToDictionary(k => k.Key,
                    v => v.Value?.ToString());


            return new FormUrlEncodedContent(dic);

            string Encode(string data)
            {
                return string.IsNullOrEmpty(data) ? string.Empty : Uri.EscapeDataString(data).Replace("%20", "+");
            }
        }
        public static string StripHtmlTags(this string source)  
        {  
            return Regex.Replace(source, "<.*?>|&.*?;", string.Empty);  
        }  
    }

    public class PropertyInformation
    {
        public PropertyInformation(PropertyInfo[] properties, Dictionary<string, Func<object, object>> valueGetter,
            Dictionary<string, Action<object, object>> valueSetter)
        {
            Properties = properties;
            ValueGetterByName = valueGetter;
            ValueSetterByName = valueSetter;
        }

        public PropertyInfo[] Properties { get; }
        public Dictionary<string, Func<object, object>> ValueGetterByName { get; }
        public Dictionary<string, Action<object, object>> ValueSetterByName { get; }
    }
}