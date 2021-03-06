using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public partial class AddressableAssetSettings
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        [Serializable]
        public partial class AssetGroup
        {
            /// <summary>
            /// TODO - doc
            /// </summary>
            [SerializeField]
            internal string name;
            /// <summary>
            /// TODO - doc
            /// </summary>
            [SerializeField]
            internal string guid;
            /// <summary>
            /// TODO - doc
            /// </summary>
            internal string displayName { get { return name; } }
            private Dictionary<string, AssetEntry> entryMap = new Dictionary<string, AssetEntry>();
            [SerializeField]
            private List<AssetEntry> m_serializeEntries = new List<AssetEntry>();
            private AddressableAssetSettings m_settings;
            /// <summary>
            /// TODO - doc
            /// </summary>
            internal Dictionary<string, AssetEntry>.ValueCollection entries
            {
                get { return entryMap.Values;  }
            }
            /// <summary>
            /// TODO - doc
            /// </summary>
            [SerializeField]
            internal AssetGroupProcessor processor;
            /// <summary>
            /// TODO - doc
            /// </summary>
            [SerializeField]
            internal bool isDefault = false;
            /// <summary>
            /// TODO - doc
            /// </summary>
            [SerializeField]
            internal bool readOnly;

            internal void OnBeforeSerialize(AddressableAssetSettings settings)
            {
                m_serializeEntries.Clear();
                foreach (var e in entries)
                    m_serializeEntries.Add(e);
            }

            internal void SerializeForHash(BinaryFormatter formatter, Stream stream)
            {
                formatter.Serialize(stream, name);
                formatter.Serialize(stream, guid);
                formatter.Serialize(stream, entries.Count);
                foreach (var e in entries)
                    e.SerializeForHash(formatter, stream);
                formatter.Serialize(stream, isDefault);
                formatter.Serialize(stream, readOnly);
                formatter.Serialize(stream, processor.GetType().FullName);
                processor.SerializeForHash(formatter, stream);
            }

            internal void OnAfterDeserialize(AddressableAssetSettings settings, Dictionary<string, AssetGroup.AssetEntry> allEntries)
            {
                m_settings = settings;
                foreach (var e in m_serializeEntries)
                {
                    e.parentGroup = this;
                    e.isSubAsset = false;
                    entryMap.Add(e.guid, e);
                    allEntries.Add(e.guid, e);
                }
                m_serializeEntries.Clear();
            }

            /// <summary>
            /// TODO - doc
            /// </summary>
            internal AssetGroup() {}
            internal AssetGroup(string n, AssetGroupProcessor p, bool setAsDefault, string g)
            {
                name = n;
                processor = p;
                isDefault = setAsDefault;
                guid = g;
                readOnly = false;
            }

            internal void ReplaceProcessor(AssetGroupProcessor proc, string newGUID)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEditor.AssetDatabase.DeleteAsset(path);
                processor = proc;
                guid = newGUID;
            }

            /// <summary>
            /// TODO - doc
            /// </summary>
            internal void AddAssetEntry(AssetEntry e)
            {
                e.isSubAsset = false;
                e.parentGroup = this;
                entryMap[e.guid] = e;
                PostModificationEvent(ModificationEvent.EntryAdded, e);
            }

            /// <summary>
            /// TODO - doc
            /// </summary>
            internal AssetEntry GetAssetEntry(string guid)
            {
                if (entryMap.ContainsKey(guid))
                    return entryMap[guid];
                return null;
            }

            internal void PostModificationEvent(ModificationEvent e, object o)
            {
                if (m_settings != null)
                    m_settings.PostModificationEvent(e, o);
            }

            /// <summary>
            /// TODO - doc
            /// </summary>
            internal void RemoveAssetEntry(AssetEntry entry)
            {
                entryMap.Remove(entry.guid);
                PostModificationEvent(ModificationEvent.EntryRemoved, entry);
            }
        }
    }
}
