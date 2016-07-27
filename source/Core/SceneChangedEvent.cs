
namespace Sol2E.Core
{
    public enum SceneEventType
    {
        EntityAdded,
        EntityRemoved
    }

    public delegate void SceneChangedEventHandler(Scene sender, SceneEventType eventType, Entity entity);

    /// <summary>
    /// Static event, where clients can register to, to listen for scene changes.
    /// Scene changes occur if an entity has been added or removed.
    /// </summary>
    public static class SceneChangedEvent
    {
        public static event SceneChangedEventHandler SceneChanged;

        public static void Invoke(Scene sender, SceneEventType eventType, Entity entity)
        {
            if (SceneChanged == null || sender == null)
                return;

            // only fire if change took place in either current or global scene
            if (sender == Scene.Current || sender == Scene.Global)
                SceneChanged.Invoke(sender, eventType, entity);
        }
    }
}
