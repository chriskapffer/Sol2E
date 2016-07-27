using System;

namespace Sol2E.Core
{
    /// <summary>
    /// Interface to provide functionality of xna's resource manager, but without
    /// explicitly using it to avoid dependencies inside this assembly.
    /// </summary>
    public interface IResourceManager : IDisposable
    {
        T Load<T>(string assetName);

        void UnloadAll();
    }
}
