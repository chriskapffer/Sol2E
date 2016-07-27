using System;
using Sol2E.Core;

namespace Sol2E.Common
{
    /// <summary>
    /// Component to decorate the hosting entity with additional information,
    /// e.g. for better lookup.
    /// </summary>
    [Serializable]
    public class EntityInfo : Component
    {
        public EntityInfo()
        {

        }

        public EntityInfo(string name, object data = null)
        {
            Name = name;
            Data = data;
        }

        // use this to tag the hosting entity with a name
        // no change notification
        public string Name { get; set; }
        // use this to add what ever info you need to the hosting entity
        // no change notification
        public object Data { get; set; }
    }
}
