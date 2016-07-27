using System;
using Sol2E.Utils;

namespace Sol2E.Core
{
    /// <summary>
    /// Abstract implementation of IDomainSystem, to decorate it with the disposable pattern
    /// and a profiler to measure the update calls.
    /// </summary>
    public abstract class AbstractDomainSystem : IDomainSystem
    {
        // profiler to watch the systems update duration
        private readonly Profiler _updateProfiler;
        // reference to the resource manager of current scene
        protected IResourceManager CurrentResourceManager { get; private set; }

        #region Instance lifecycle Methods

        protected AbstractDomainSystem(string domainName)
        {
            _updateProfiler = new Profiler(domainName + "Update");
        }

        ~AbstractDomainSystem()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        public void Dispose()
        {
            // Dispose calls Dispose(true)
            Dispose(true);
            // Suppress garbage collection
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Calls protected Update method inside a ProfileContext.
        /// </summary>
        /// <param name="elapsedGameTime">elapsed game time</param>
        public void Update(TimeSpan elapsedGameTime)
        {
            using (new ProfileContext(_updateProfiler))
            {
                Update((float)elapsedGameTime.TotalSeconds);
            }
        }

        #region Abstract Methods

        /// <summary>
        /// Initializes internal resources, which might be not available at creation.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Protected update method. Do your logic here.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected abstract void Update(float deltaTime);

        /// <summary>
        /// Protected dispose method. Do your clean up here.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// This method should register to ScenesWillSwitchEvents and handle them appropriately.
        /// </summary>
        /// <param name="oldScene">scene which was active when the method was called</param>
        /// <param name="newScene">scene which will become active when the method returned</param>
        /// <param name="newResourceManager">resource manager, associated with newScene</param>
        public void ScenesWillSwitch(Scene oldScene, Scene newScene, IResourceManager newResourceManager)
        {
            if (oldScene != null)
            {
                foreach (Entity entity in oldScene.GetAll())
                {
                    SceneChanged(oldScene, SceneEventType.EntityRemoved, entity);
                }
            }

            CurrentResourceManager = newResourceManager;

            if (newScene != null)
            {
                foreach (Entity entity in newScene.GetAll())
                {
                    SceneChanged(newScene, SceneEventType.EntityAdded, entity);
                }
            }

            CleanUpAfterSceneSwitch();
        }

        /// <summary>
        /// This method should register to SceneChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected scene</param>
        /// <param name="eventType">whether an entity was added or removed</param>
        /// <param name="entity">affected entity</param>
        public abstract void SceneChanged(Scene sender, SceneEventType eventType, Entity entity);

        /// <summary>
        /// This method should register to EntityChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected entity</param>
        /// <param name="eventType">whether a component was added, removed or deserialized</param>
        /// <param name="component">affected component</param>
        public abstract void EntityChanged(Entity sender, EntityEventType eventType, Component component);

        /// <summary>
        /// This gives the opportunity for additional clean up after a scene switch.
        /// </summary>
        protected virtual void CleanUpAfterSceneSwitch()
        {
            // perform any needed cleanup in subclass
        }

        #endregion
    }
}
