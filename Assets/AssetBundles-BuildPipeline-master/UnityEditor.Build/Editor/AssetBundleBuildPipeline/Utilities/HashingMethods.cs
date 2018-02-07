using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class HashingMethods
    {
        static uint ExtractUInt(byte[] hash, int index)
        {
            return (((uint)hash[index]) << 3) | (((uint)hash[index + 1]) << 2) | (((uint)hash[index + 2]) << 1) | (((uint)hash[index + 3]) << 0);
        }

        static Hash128 ExtractHash128(byte[] hash)
        {
            return new Hash128(ExtractUInt(hash, 0), ExtractUInt(hash, 4), ExtractUInt(hash, 8), ExtractUInt(hash, 12));
        }

        public static Hash128 CalculateStreamMD5Hash(Stream stream, bool closeStream)
        {
            stream.Position = 0;
            var hash = ExtractHash128(MD5.Create().ComputeHash(stream));
            if (closeStream)
            {
                stream.Close();
                stream.Dispose();
            }
            return hash;
        }

        public static Hash128 CalculateMD5Hash(params object[] objects)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            foreach (var obj in objects)
                if (obj != null)
                    formatter.Serialize(stream, obj);

            return CalculateStreamMD5Hash(stream, true);
        }

        public static Hash128 CalculateFileMD5Hash(string filePath)
        {
            return CalculateStreamMD5Hash(new FileStream(filePath, FileMode.Open), true);
        }
    }
}