using System;

namespace Sol2E.Core
{
    /// <summary>
    /// Interface declaration of a domain system. A domain system handles all the task which
    /// could appear when using components and entites of a given game domain. E.g. GraphicsSystem
    /// handles every entity and component which affects the scene to display. Other examples would
    /// be InputSystem, PhysicsSystem, AudioSystem, etc. They all have to implement this interface.
    /// It also inherits the IDisposable interface, to give clients the change to clean up their
    /// resources.
    /// </summary>
    public interface IDomainSystem : IDisposable
    {
        // usual xna suspects
        void Initialize();
        void Update(TimeSpan elapsedGameTime);

        /// <summary>
        /// This method should register to ScenesWillSwitchEvents and handle them appropriately.
        /// </summary>
        /// <param name="oldScene">scene which was active when the method was called</param>
        /// <param name="newScene">scene which will become active when the method returned</param>
        /// <param name="newResourceManager">resource manager, associated with newScene</param>
        void ScenesWillSwitch(Scene oldScene, Scene newScene, IResourceManager newResourceManager);

        /// <summary>
        /// This method should register to SceneChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected scene</param>
        /// <param name="eventType">whether an entity was added or removed</param>
        /// <param name="entity">affected entity</param>
        void SceneChanged(Scene sender, SceneEventType eventType, Entity entity);

        /// <summary>
        /// This method should register to EntityChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected entity</param>
        /// <param name="eventType">whether a component was added, removed or deserialized</param>
        /// <param name="component">affected component</param>
        void EntityChanged(Entity sender, EntityEventType eventType, Component component);
    }
}
