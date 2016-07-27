
namespace Sol2E.Core
{
    public enum EntityEventType
    {
        ComponentAdded,
        ComponentRemoved,
        ComponentDeserialized
    }

    public delegate void EntityChangedEventHandler(Entity sender, EntityEventType eventType, Component component);

    /// <summary>
    /// Static event, where clients can register to, to listen for entity changes.
    /// Entity changes occur if a component has been added or removed or deserialized.
    /// </summary>
    public static class EntityChangedEvent
    {
        public static event EntityChangedEventHandler EntityChanged;

        public static void Invoke(Entity sender, EntityEventType eventType, Component component)
        {
            if (EntityChanged == null || sender == null)
                return;

            // only fire if entity belongs to either current or global scene
            var hostingScene = sender.GetHostingScene();
            if (hostingScene != null && (hostingScene == Scene.Current || hostingScene == Scene.Global))
                EntityChanged.Invoke(sender, eventType, component);
        }
    }
}
