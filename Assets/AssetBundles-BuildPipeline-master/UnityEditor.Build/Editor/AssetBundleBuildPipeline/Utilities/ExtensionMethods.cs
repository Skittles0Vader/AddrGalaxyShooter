using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class ExtensionMethods
    {
        public static bool IsNullOrEmpty<T> (this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static void GetOrAdd<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key, out List<TValue> value)
        {
            if (dictionary.TryGetValue(key, out value))
                return;

            value = new List<TValue>();
            dictionary.Add(key, value);
        }

        public static void GetOrAdd<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dictionary, TKey key, out HashSet<TValue> value)
        {
            if (dictionary.TryGetValue(key, out value))
                return;

            value = new HashSet<TValue>();
            dictionary.Add(key, value);
        }

        public static void Swap<T>(this IList<T> array, int index1, int index2)
        {
            var t = array[index2];
            array[index2] = array[index1];
            array[index1] = t;
        }

        public static void Swap<T>(this T[] array, int index1, int index2)
        {
            var t = array[index2];
            array[index2] = array[index1];
            array[index1] = t;
        }

        public static void UnionWith(this Experimental.Build.AssetBundle.BuildUsageTagSet usageSet, Experimental.Build.AssetBundle.BuildUsageTagSet other)
        {
            // NO OP FOR NOW
        }

        public static Hash128 GetUsageHashForObjectIdentifier(this Experimental.Build.AssetBundle.BuildUsageTagSet usageSet, ObjectIdentifier objectID)
        {
            // NO OP FOR NOW
            return HashingMethods.CalculateMD5Hash(objectID);
        }

        public static Hash128 GetUsageHashForObjectIdentifiers(this Experimental.Build.AssetBundle.BuildUsageTagSet usageSet, ObjectIdentifier[] objectIDs)
        {
            // NO OP FOR NOW
            return HashingMethods.CalculateMD5Hash(objectIDs);
        }
    }
}
