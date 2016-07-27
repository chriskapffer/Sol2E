using Microsoft.Xna.Framework.Content;
using Sol2E.Core;

namespace Sol2E
{
    /// <summary>
    /// Implementation of IResourceManager to wrap xna's content manager in.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        // content manager intance, which performs all the action
        private readonly ContentManager _contentManager;

        public string ResourceDirectory
        {
            get { return _contentManager.RootDirectory; }
            set { _contentManager.RootDirectory = value; }
        }

        public ResourceManager(ContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public T Load<T>(string assetName)
        {
            //if (assetName == null || assetName == string.Empty)
            //    return default(T);

            return _contentManager.Load<T>(assetName);
        }

        public void UnloadAll()
        {
            _contentManager.Unload();
        }

        public void Dispose()
        {
            _contentManager.Unload();
            _contentManager.Dispose();
        }
    }
}
