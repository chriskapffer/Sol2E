using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Sol2E.Utils;

namespace Sol2E.Core
{
    /// <summary>
    /// An abstract base class for all kinds of components.
    /// It doesn't contain any fields except for one single identifier.
    /// 
    /// It provides basic common methods for data acccess as well as lifecycle,
    /// ownership and data management. 
    /// The methods themself do not perform any actions, but wrap the functionality
    /// of the unterlying database to make it accessible in a way to be easily
    /// understood by clients.
    /// 
    /// A class derived from this, shouldn't have to implement anything else but
    /// public properties. However those properties have to invoke the
    /// ComponentChangedEvent if clients should be notified about their changes.
    /// </summary>
    [Serializable]
    public abstract class Component
    {
        #region Fields

        // identifier
        public int Id { get; internal set; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Creates a new instance of given type.
        /// </summary>
        /// <returns>new instance</returns>
        public static T Create<T>() where T : Component
        {
            return InstanceCreator.CreateInstanceFromDefaultContructor<T>();
        }

        /// <summary>
        /// Destroys the given component.
        /// </summary>
        /// <param name="component">component to destroy</param>
        public static void Destroy(Component component)
        {
            // fire entity changed event. If component doesn't belong to a entity, nothing happens. 
            EntityChangedEvent.Invoke(component.GetHostingEntity(), EntityEventType.ComponentRemoved, component);

            Database.Instance.ComponentUnRegister(component);
        }

        // protected constructor, since this is an abstract class
        protected Component()
        {
            Database.Instance.ComponentRegister(this);
        }

        #endregion

        #region Instance Access Methods

        /// <summary>
        /// Returns the component instance associated with the given id.
        /// </summary>
        /// <param name="componentId">id of component to get</param>
        /// <returns>component associated with given id</returns>
        public static Component GetInstance(int componentId)
        {
            return Database.Instance.ComponentGetInstance(componentId);
        }

        /// <summary>
        /// Returns all instances of given type.
        /// </summary>
        /// <typeparam name="T">component type param</typeparam>
        /// <returns>all instances of given type</returns>
        public static IEnumerable<T> GetAll<T>() where T : Component
        {
            return Database.Instance.ComponentGetAllInstances<T>(typeof(T));
        }

        /// <summary>
        /// Returns the number of components in this game.
        /// </summary>
        /// <returns>number of components</returns>
        public static int Count()
        {
            return Database.Instance.ComponentCount;
        }

        /// <summary>
        /// Returns the number of components in given scene.
        /// </summary>
        /// <param name="scene">scene to inspect</param>
        /// <returns>number of components in scene</returns>
        public static int Count(Scene scene)
        {
            return Database.Instance.ComponentCountInScene(scene);
        }

        /// <summary>
        /// Returns the number of components of given type in this game.
        /// </summary>
        /// <typeparam name="T">component type param</typeparam>
        /// <returns>number of components of type</returns>
        public static int Count<T>()
        {
            return Database.Instance.ComponentCountOfType(typeof(T));
        }

        /// <summary>
        /// Returns the number of components of given type in given scene.
        /// </summary>
        /// <typeparam name="T">component type param</typeparam>
        /// <param name="scene">scene to inspect</param>
        /// <returns>number of components of type in scene</returns>
        public static int Count<T>(Scene scene)
        {
            return Database.Instance.ComponentCountOfTypeInScene(typeof(T), scene);
        }

        #endregion

        #region Ownership Methods

        /// <summary>
        /// Assigns this component to given entity.
        /// </summary>
        /// <param name="entity">entity to assign this component to</param>
        public void AssignToEntity(Entity entity)
        {
            var oldEntity = GetHostingEntity();
            if (oldEntity == entity)
                return;

            EntityChangedEvent.Invoke(oldEntity, EntityEventType.ComponentRemoved, this);

            Database.Instance.ComponentAssignToEntity(GetType(), Id, entity.Id);

            EntityChangedEvent.Invoke(entity, EntityEventType.ComponentAdded, this);
        }

        /// <summary>
        /// Returns the entity this component is currently assigned to.
        /// </summary>
        /// <returns>instance of hosting entity</returns>
        public Entity GetHostingEntity()
        {
            int entityId = Database.Instance.ComponentGetIdOfHostingEntity(GetType(), Id);
            return (entityId != IDPool.InvalidID) ? Entity.GetInstance(entityId) : null;
        }

        #endregion

        #region Data Management Methods

        /// <summary>
        /// Called if component got deserialized.
        /// Invokes an EntityChangedEvent to notify listeners about a possible change.
        /// </summary>
        /// <param name="context">streamingContext (not used)</param>
        [OnDeserialized]
        private void OnSerializedMethod(StreamingContext context)
        {
            var entity = GetHostingEntity();
            EntityChangedEvent.Invoke(entity, EntityEventType.ComponentDeserialized, this);
        }

        #endregion
    }
}
