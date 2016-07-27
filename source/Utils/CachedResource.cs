using System;
using System.Runtime.Serialization;

namespace Sol2E.Utils
{
    /// <summary>
    /// Wrapper class to fake serialization of managed resources. Use this, if the underlying
    /// class is not serializable.
    /// 
    /// Caution! This works only within one application life cycle. If you want to use this
    /// to deserialize resources from persistent storage, it won't work, because the cache only
    /// contains the resource if it was serialized between application start and deserialization
    /// request.
    /// </summary>
    /// <typeparam name="T">type of cached resource (has to be a reference type)</typeparam>
    [Serializable]
    public class CachedResource<T> where T : class
    {
        [NonSerialized]
        private T _resource;
        public T Resource
        {
            get
            {
                return _resource;
            }
            set
            {
                if (_resource != value)
                {
                    _resource = value;

                    // get an id, if the resource is accessed for the first time
                    if (Id == IDPool.InvalidID && _resource != null)
                        // use class type as key for multiton key
                        Id = IDPool.GetInstance(typeof(Cache)).GetNextAvailableID(Cache.Keys);

                    // if null was assigned to the resource, we assume it is not
                    // needed anymore and remove it from cache
                    if (Id != IDPool.InvalidID && _resource == null)
                        Cache.Remove(Id);
                }
            }
        }

        // identifier to retrieve resource from cache.
        public int Id { get; private set; }

        public CachedResource()
        {
            Id = IDPool.InvalidID;
            Resource = default(T);
        }

        /// <summary>
        /// Gets called during serialization. Adds the unserializable
        /// resource to temporary cache.
        /// </summary>
        /// <param name="context">streamingContext (not used)</param>
        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (Cache.Enabled && Resource != null)
                Cache.Add(Id, Resource);
        }

        /// <summary>
        /// Gets called after deserialization. Removes the unserializable
        /// resource from temporary cache.
        /// </summary>
        /// <param name="context">streamingContext (not used)</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Cache.Enabled)
                Resource = Cache.Remove(Id) as T;
        }
    }
}
