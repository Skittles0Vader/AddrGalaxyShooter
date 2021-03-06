using System.Collections.Generic;

namespace ResourceManagement
{
    /// <summary>
    /// Contains enough information to load an asset (what/where/how/dependencies)
    /// </summary>
    public interface IResourceLocation
    {
        /// <summary>
        /// Internal name used by the provider to load this location
        /// </summary>
        /// <value>The identifier.</value>
        string id { get; }

        /// <summary>
        /// Matches the provider used to provide/load this location
        /// </summary>
        /// <value>The provider id.</value>
        string providerId { get; }

        /// <summary>
        /// Gets the dependencies to other IResourceLocations
        /// </summary>
        /// <value>The dependencies.</value>
        IList<IResourceLocation> dependencies { get; }
    }

    /// <summary>
    /// Resource location with an additional typed key.
    /// </summary>
    public interface IResourceLocation<TKey> : IResourceLocation
    {
        /// <summary>
        /// Gets the key for the location.
        /// </summary>
        /// <value>The key.</value>
        TKey key { get; }
    }
}
