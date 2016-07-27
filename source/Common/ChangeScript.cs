using System;
using Sol2E.Core;

namespace Sol2E.Common
{
    /// <summary>
    /// Abstract script, which listens for a ComponentChangedEvent of type T and executes OnChange
    /// </summary>
    /// <typeparam name="T">type param to listen for changes for (has to be of type Component)</typeparam>
    [Serializable]
    public abstract class ChangeScript<T> : ScriptCollectionItem where T : Component
    {
        protected ChangeScript()
        {
            ComponentChangedEvent<T>.ComponentChanged += OnChange;
        }

        public abstract void OnChange(T sender, string propertyName, object oldValue);
    }
}
