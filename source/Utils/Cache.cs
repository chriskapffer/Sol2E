using System;
using System.Collections.Generic;

namespace Sol2E.Utils
{
    /// <summary>
    /// Helper to be able to use using blocks. Ensures that the
    /// cache gets disabled after exiting context.
    /// Use it like this:
    /// 
    /// using (new CachingEnabled())
    /// {
    ///     Cache.Add(idOfResource, resourceToCache);
    /// }
    /// </summary>
    public class CachingEnabled : IDisposable
    {
        public CachingEnabled()
        {
            Cache.Enabled = true;
        }

        public void Dispose()
        {
            Cache.Enabled = false;
        }
    }

    /// <summary>
    /// Class to cache unmanaged resources during serialization and deserialization.
    /// </summary>
    public static class Cache
    {
        // if false, Add and Remove won't do anything 
        public static bool Enabled { get; internal set; }

        // dictionary to store cached resources
        private static readonly IDictionary<int, object> Resources = new Dictionary<int, object>();
        internal static ICollection<int> Keys
        {
            get { return Resources.Keys; }
        }

        /// <summary>
        /// Adds a resource to the cache, or reassignes it, if already existent.
        /// </summary>
        /// <param name="id">identifier used for storage</param>
        /// <param name="obj">resource to cache</param>
        internal static void Add(int id, object obj)
        {
            if (!Enabled)
                return;

            if (!Resources.ContainsKey(id))
            {
                Resources.Add(id, obj);
            }
            else
            {
                Resources[id] = obj;
            }
        }

        /// <summary>
        /// Removes/Pops a resource from the cache
        /// </summary>
        /// <param name="id">identifier used for storage</param>
        /// <returns>the cached resource</returns>
        internal static object Remove(int id)
        {
            if (!Enabled)
                return null;

            object obj;
            if (Resources.TryGetValue(id, out obj))
            {
                Resources.Remove(id);
            }
            return obj;
        }

        /// <summary>
        /// Clears the entire cache
        /// </summary>
        public static void Clear()
        {
            Resources.Clear();
        }
    }
}
