using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace AddressableAssets.LivePreview
{
    [Serializable]
    public class Session
    {
        [SerializeField] public string sessionID;
        [SerializeField] public BuildTarget buildTarget;
        [SerializeField] public BuildTargetGroup buildTargetGroup;
        [SerializeField] public TypeDB typeDB;

        public const string kSessionDirectory = "SessionData";
        public const string kFilename = "Session.bin";
        public const string kPlayerDataDirectory = "PlayerData";
        public const string kDynamicDataDirectory = "PlayerData\\Data\\DynamicData";

        internal Session() { }

        public static string SerializedDirectoryForSessionID(string sessionID)
        {
            return Path.Combine(kSessionDirectory, sessionID.ToString());
        }

        public static string GetPlayerDataDirectory(string sessionID)
        {
            string playerDataDirectory = Path.Combine(SerializedDirectoryForSessionID(sessionID), kPlayerDataDirectory);
            return playerDataDirectory;
        }

        public static string GetDynamicDataDirectory(string sessionID)
        {
            string dynamicDataDir = Path.Combine(SerializedDirectoryForSessionID(sessionID), kDynamicDataDirectory);
            Directory.CreateDirectory(dynamicDataDir);
            return dynamicDataDir;
        }

        public string SerializedDirectory
        {
            get { return SerializedDirectoryForSessionID(sessionID); }
        }

        private string SerializedObjectPath
        {
            get { return Path.Combine(SerializedDirectory, kFilename); }
        }

        public static Session CreateFromId(string SessionId)
        {
            string dir = Path.Combine(Session.kSessionDirectory, SessionId);
            return CreateFromDirectory(dir);
        }

        public static Session CreateFromDirectory(string directory)
        {
            try
            {
                string filename = Path.Combine(directory, kFilename);
                using (FileStream stream = File.OpenRead(filename))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Session session = (Session)formatter.Deserialize(stream);
                    return session;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public void Save()
        {
            if (!Directory.Exists(kSessionDirectory))
                Directory.CreateDirectory(kSessionDirectory);
            if (!Directory.Exists(SerializedDirectory))
                Directory.CreateDirectory(SerializedDirectory);

            using (FileStream stream = File.OpenWrite(SerializedObjectPath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
            }
        }

        // VFS file request, return the absolute path of the file
        public string RequestFile(string filename)
        {
            string playerDataFilename = Path.Combine(SerializedDirectory, kPlayerDataDirectory);
            string result = Path.Combine(playerDataFilename, filename);
            return result;
        }
    }
}