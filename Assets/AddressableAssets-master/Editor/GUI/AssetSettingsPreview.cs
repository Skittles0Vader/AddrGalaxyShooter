using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using UnityEditor.TreeViewExamples;
using System.IO;
using ResourceManagement.ResourceProviders;

namespace AddressableAssets
{
    [Serializable]
    internal class AssetSettingsPreview
    {
        [SerializeField]
        TreeViewState treeState;
        AssetSettingsPreviewTreeView tree;

        [SerializeField]
        internal UnityEditor.Build.BuildDependencyInfo buildInfo;

        [SerializeField]
        internal HashSet<GUID> explicitAssets;

        internal AssetSettingsPreview()
        {
        }

        private Texture2D m_RefreshTexture;
        internal Texture2D bundleIcon;
        internal Texture2D sceneIcon;

        private void FindBundleIcons()
        {
            string[] icons = AssetDatabase.FindAssets("AddressableAssetsIconY1756");
            foreach (string i in icons)
            {
                string name = AssetDatabase.GUIDToAssetPath(i);
                if (name.Contains("AddressableAssetsIconY1756Basic.png"))
                    bundleIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
                else if (name.Contains("AddressableAssetsIconY1756Scene.png"))
                    sceneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
            }
        }

        internal void OnGUI(Rect pos)
        {
            if (tree == null)
            {
                if (treeState == null)
                    treeState = new TreeViewState();

                tree = new AssetSettingsPreviewTreeView(treeState, this);
                tree.Reload();


                m_RefreshTexture = EditorGUIUtility.FindTexture("Refresh");
                FindBundleIcons();
            }

            GUILayout.Space(4);
            if (GUILayout.Button(m_RefreshTexture, GUILayout.ExpandWidth(false)))
            {
                ReloadPreview();
            }

            tree.OnGUI(new Rect(pos.x, pos.y + 32, pos.width, pos.height - 28));
        }

        private void ReloadPreview()
        {
            explicitAssets = new HashSet<GUID>();

            buildInfo = BuildScript.PreviewDependencyInfo();
            if (buildInfo != null)
            {
                foreach (var b in buildInfo.bundleToAssets)
                {
                    foreach (var g in b.Value)
                    {
                        explicitAssets.Add(g);
                    }
                }
            }
            else
                Debug.LogError("Build preview failed.");

            tree.Reload();
        }
    }

    internal class AssetSettingsPreviewTreeView : TreeView
    {
        AssetSettingsPreview preview;
        internal AssetSettingsPreviewTreeView(TreeViewState state, AssetSettingsPreview prev) : base(state)
        {
            showBorder = true;
            preview = prev;
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem(-1, -1);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            List<TreeViewItem> tempRows = new List<TreeViewItem>(10);
            if (preview.buildInfo != null)
            {
                foreach (var bundleAssets in preview.buildInfo.bundleToAssets)
                {
                    var bundleItem = new TreeViewItem(bundleAssets.Key.GetHashCode(), 0, bundleAssets.Key);
                    bundleItem.icon = preview.bundleIcon;
                    tempRows.Add(bundleItem);
                    if (bundleAssets.Value.Count > 0)
                    {
                        if (IsExpanded(bundleItem.id))
                        {
                            foreach (var g in bundleAssets.Value)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(g.ToString());
                                var assetItem = new TreeViewItem(path.GetHashCode(), 1, path);
                                assetItem.icon = AssetDatabase.GetCachedIcon(path) as Texture2D;
                                tempRows.Add(assetItem);
                                bundleItem.AddChild(assetItem);

                                UnityEditor.Experimental.Build.AssetBundle.AssetLoadInfo loadInfo;
                                if (preview.buildInfo.assetInfo.TryGetValue(g, out loadInfo) && loadInfo.referencedObjects.Count() > 0)
                                {
                                    if (IsExpanded(assetItem.id))
                                    {
                                        foreach (var r in loadInfo.referencedObjects)
                                        {
                                            if ((!preview.explicitAssets.Contains(r.guid)) &&
                                                (r.filePath != "library/unity default resources") &&
                                                (r.filePath != "resources/unity_builtin_extra"))
                                            {
                                                var subpath = AssetDatabase.GUIDToAssetPath(r.guid.ToString());
                                                var subAssetItem = new TreeViewItem(subpath.GetHashCode(), 2, subpath);
                                                subAssetItem.icon = AssetDatabase.GetCachedIcon(subpath) as Texture2D;
                                                tempRows.Add(subAssetItem);
                                                assetItem.AddChild(subAssetItem);
                                            }
                                        }
                                    }
                                    else
                                        assetItem.children = CreateChildListForCollapsedParent();
                                }
                            }
                        }
                        else
                            bundleItem.children = CreateChildListForCollapsedParent();
                    }
                }
            }


            return tempRows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if ((args.selected == false) &&
                (Event.current.type == EventType.Repaint))
            {
                if (args.item.depth % 2 == 0)
                    DefaultStyles.backgroundOdd.Draw(args.rowRect, false, false, false, false);
                else
                    DefaultStyles.backgroundEven.Draw(args.rowRect, false, false, false, false);
            }
            using (new EditorGUI.DisabledScope(args.item.depth >= 2))
                base.RowGUI(args);
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            //temporarily removing due to "hot control" issue.
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }
    }
}
