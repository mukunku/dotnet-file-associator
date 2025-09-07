using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

namespace DotnetFileAssociator
{
    public static class ExtensionMethods
    {
        //Source: https://stackoverflow.com/a/7574615/1458738
        public static string Left(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) 
                return value;

            maxLength = Math.Abs(maxLength);

            return (value.Length <= maxLength
                   ? value
                   : value.Substring(0, maxLength)
                   );
        }

        public static T Last<T>(this ICollection collection)
        {
            if (collection.Count == 0)
                throw new InvalidOperationException("Collection contains no elements");

            object? lastItem = null;
            foreach(var item in collection)
            {
                lastItem = item;
            }

            return (T)Convert.ChangeType(lastItem, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T Get<T>(this ICollection collection, int index)
        {
            if (index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            foreach(var key in collection)
            {
                if (0 == index)
                {
                    return (T)Convert.ChangeType(key, typeof(T), CultureInfo.InvariantCulture);
                }
                index--;
            }

            throw new InvalidOperationException("We should never get here");
        }

        public static IEnumerable<(T Key, R Value)> AsEnumerable<T, R>(this OrderedDictionary orderedDictionary)
        {
            for(var i = 0; i < orderedDictionary.Count; i++)
            {
                var key = orderedDictionary.Keys.Get<T>(i);
                var value = orderedDictionary.Values.Get<R>(i);
                yield return (key, value);
            }
        }
    }
}
