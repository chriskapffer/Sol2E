
namespace Sol2E.Core
{
    public delegate void ComponentChangedEventHandler<in T>(T sender, string propertyName, object oldValue) where T : Component;

    /// <summary>
    /// Static event, where clients can register to, to listen for component changes.
    /// Component changes occur if any of its properties have been modified.
    /// </summary>
    public static class ComponentChangedEvent<T> where T : Component
    {
        public static event ComponentChangedEventHandler<T> ComponentChanged;

        public static void Invoke(T sender, string propertyName, object oldValue = null)
        {
            if (ComponentChanged == null || sender == null)
                return;

            // don't fire if component is unassigned.
            var hostingEntity = sender.GetHostingEntity();
            if (hostingEntity == null)
                return;

            // only fire if entity belongs to either current or global scene
            var hostingScene = hostingEntity.GetHostingScene();
            if (hostingScene != null && (hostingScene == Scene.Current || hostingScene == Scene.Global))
                ComponentChanged.Invoke(sender, propertyName, oldValue);
        }
    }
}
