using System;
using System.Collections.Generic;
using ResourceManagement;
using Object = UnityEngine.Object;
using UnityEngine;

namespace AddressableAssets
{
    /// <summary>
    /// Entry point for Addressable API, this provides a simpler interface than using ResourceManager directly as it assumes string address type.
    /// </summary>
    public class Addressables
    {
        /// <summary>
        /// Return all addresses that have a given label
        /// </summary>
        static public IList<TAddress> GetAddresses<TAddress>(string label)
        {
            for (int i = 0; i < ResourceManager.resourceLocators.Count; i++)
            {
                var locator = ResourceManager.resourceLocators[i] as ResourceLocationMap<TAddress>;
                if (locator == null)
                    continue;

                var l = locator.GetAddresses(label);
                if (l != null)
                    return l;
            }
            return null;
        }


        /// <summary>
        /// Release a loaded asset.  The asset is ref counted so it may not be unloaded immediately.
        /// </summary>
        public static void Release<TObject>(TObject obj) where TObject : class
        {
            ResourceManager.Release<TObject>(obj);
        }


        /// <summary>
        /// Release an instantiated object.  The object may be released to an object pool so it may not be detroyed immediately.  The asset that it was instantiated from will have its ref count decreased, which may unload the asset.
        /// </summary>
        public static void ReleaseInstance<TObject>(TObject obj) where TObject : Object
        {
            ResourceManager.ReleaseInstance<TObject>(obj);
        }
 
        /// <summary>
        /// Load an asset via a string address.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> LoadAsync<TObject>(string address) where TObject : class
        {
            return ResourceManager.LoadAsync<TObject, string>(address);
        }
        /// <summary>
        /// Load an asset via an AssetReference.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> LoadAsync<TObject>(AssetReference assetRef) where TObject : class
        {
            return ResourceManager.LoadAsync<TObject, AssetReference>(assetRef);
        }


        /// <summary>
        /// Load multiple assets from a list of string addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<TObject>> LoadAllAsync<TObject>(IList<string> addresses, Action<IAsyncOperation<TObject>> callback) where TObject : class
        {
            return ResourceManager.LoadAllAsync<TObject, string>(addresses, callback);
        }
        /// <summary>
        /// Load multiple assets from a list of AssetReference addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<TObject>> LoadAllAsync<TObject>(IList<AssetReference> assetRefs, Action<IAsyncOperation<TObject>> callback) where TObject : class
        {
            return ResourceManager.LoadAllAsync<TObject, AssetReference>(assetRefs, callback);
        }

        /// <summary>
        /// Instantiate an asset via a string address.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> InstantiateAsync<TObject>(string address, Transform parent = null, bool instantiateInWorldSpace = false) where TObject : Object
        {
            return ResourceManager.InstantiateAsync<TObject, string>(address, parent, instantiateInWorldSpace);
        }
        /// <summary>
        /// Instantiate an asset via a string address.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> InstantiateAsync<TObject>(string address, Vector3 position, Quaternion rotation, Transform parent = null) where TObject : Object
        {
            return ResourceManager.InstantiateAsync<TObject, string>(address, position, rotation, parent);
        }

        /// <summary>
        /// Load an asset via an AssetReference address.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> InstantiateAsync<TObject>(AssetReference address, Transform parent = null, bool instantiateInWorldSpace = false) where TObject : Object
        {
            return ResourceManager.InstantiateAsync<TObject, AssetReference>(address, parent, instantiateInWorldSpace);
        }
        /// <summary>
        /// Load an asset via an AssetReference address.  The IAsyncOperation returned can be yielded upon or a completion handler can be set to wait for the result.
        /// </summary>
        public static IAsyncOperation<TObject> InstantiateAsync<TObject>(AssetReference address, Vector3 position, Quaternion rotation, Transform parent = null) where TObject : Object
        {
            return ResourceManager.InstantiateAsync<TObject, AssetReference>(address, position, rotation, parent);
        }

        /// <summary>
        /// Instantiate multiple assets from a list of string addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject>(IList<string> addresses, Action<IAsyncOperation<TObject>> callback) where TObject : Object
        {
            return ResourceManager.InstantiateAllAsync<TObject, string>(addresses, callback);
        }
        /// <summary>
        /// Instantiate multiple assets from a list of AssetReference addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<TObject>> InstantiateAllAsync<TObject>(IList<AssetReference> addresses, Action<IAsyncOperation<TObject>> callback) where TObject : Object
        {
            return ResourceManager.InstantiateAllAsync<TObject, AssetReference>(addresses, callback);
        }

        /// <summary>
        /// Unload scene via string address.
        /// </summary>
        public static IAsyncOperation<UnityEngine.SceneManagement.Scene> UnloadSceneAsync(string address)
        {
            return ResourceManager.UnloadSceneAsync<string>(address);
        }

        /// <summary>
        /// Preload dependencies for an asset via its string address.
        /// </summary>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAsync(string address, Action<IAsyncOperation<object>> callback)
        {
            return ResourceManager.PreloadDependenciesAsync<string>(address, callback);
        }
        /// <summary>
        /// Preload dependencies for an asset via its AssetReference address.
        /// </summary>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAsync(AssetReference address, Action<IAsyncOperation<object>> callback)
        {
            return ResourceManager.PreloadDependenciesAsync<AssetReference>(address, callback);
        }

        /// <summary>
        /// Preload multiple assets from a list of string addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAllAsync(IList<string> addresses, Action<IAsyncOperation<object>> callback)
        {
            return ResourceManager.PreloadDependenciesAllAsync<string>(addresses, callback);
        }

        /// <summary>
        /// Preload multiple assets from a list of AssetReference addresses.  A callback can be passed in to handle each asset load as it completes.
        /// </summary>
        public static IAsyncOperation<IList<object>> PreloadDependenciesAllAsync(IList<AssetReference> addresses, Action<IAsyncOperation<object>> callback)
        {
            return ResourceManager.PreloadDependenciesAllAsync<AssetReference>(addresses, callback);
        }

        /// <summary>
        /// Load a scene via its string address.
        /// </summary>
        public static IAsyncOperation<UnityEngine.SceneManagement.Scene> LoadSceneAsync(string address, UnityEngine.SceneManagement.LoadSceneMode loadMode = UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            return ResourceManager.LoadSceneAsync(address, loadMode);
        }


    }
}

