using System;

namespace Sol2E.Common
{
    /// <summary>
    /// Acts more or less as an interface, but wanted to restrict accessibility of setter
    /// </summary>
    [Serializable]
    public abstract class ScriptCollectionItem
    {
        // holds the id of a ScriptCollection component
        public int IdOfHostingComponent { get; internal set; }
    }
}
