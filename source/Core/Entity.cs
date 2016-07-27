using System;
using System.Collections.Generic;
using Sol2E.Utils;

namespace Sol2E.Core
{
    /// <summary>
    /// A logical representation of an entity (or game object).
    /// 
    /// This class doesn't contain any fields except for one single identifier.
    /// However it does contain methods for data acccess as well as lifecycle,
    /// persistency, ownership and data management. 
    /// The methods themself do not perform any actions, but wrap the functionality
    /// of the unterlying database to make it accessible in a way to be easily
    /// understood by clients.
    /// 
    /// The behaviour of an entity is solely defined by the components it contains.
    /// As stated above, the entity actually does not contain any components, but they
    /// are associated to its id. These relations are managed by the database.
    /// </summary>
    [Serializable]
    public class Entity
    {
        #region Fields

        // identifier
        public int Id { get; private set; }

#if DEBUG
        // reference to database. this is only used for debugging
        // to be able to inspect it with intellisense
        [NonSerialized]
        private Database _dbRef = Database.Instance;
#endif

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Creates a new instance of entity.
        /// </summary>
        /// <returns>new instance</returns>
        public static Entity Create()
        {
            return Database.Instance.EntityCreate();
        }

        /// <summary>
        /// Destroys the given entity.
        /// </summary>
        /// <param name="entity">entity to destroy</param>
        public static void Destroy(Entity entity)
        {
            if (entity == null)
                return;

            // fire scene changed event. If entity doesn't belong to a scene, nothing happens. 
            SceneChangedEvent.Invoke(entity.GetHostingScene(), SceneEventType.EntityRemoved, entity);

            Database.Instance.EntityDestroy(entity);
        }

        // private default constructor to hide it from everybody else
        private Entity() { }

        /// <summary>
        /// Internal constructor. Gets called by EntityCreate in Database.
        /// </summary>
        /// <param name="entityId">id assigned to this entity</param>
        internal Entity(int entityId)
        {
            Id = entityId;
        }

        #endregion

        #region Instance Access Methods

        /// <summary>
        /// Returns the entity instance associated with the given id.
        /// </summary>
        /// <param name="entityId">id of entity to get</param>
        /// <returns>entity associated with given id</returns>
        public static Entity GetInstance(int entityId)
        {
            return Database.Instance.EntityGetInstance(entityId);
        }

        /// <summary>
        /// Returns all entity instances which own a component of given type.
        /// </summary>
        /// <param name="componentType">type of required component</param>
        /// <returns>list of entitis wich own the required component</returns>
        public static IEnumerable<Entity> GetInstances(Type componentType)
        {
            return Database.Instance.EntityGetInstances(componentType);
        }

        /// <summary>
        /// Returns all entity instances which own all components of given types.
        /// </summary>
        /// <param name="componentTypes">list of types of required components</param>
        /// <returns>list of entitis wich own the required components</returns>
        public static IEnumerable<Entity> GetInstances(IEnumerable<Type> componentTypes)
        {
            return Database.Instance.EntityGetInstances(componentTypes);
        }

        /// <summary>
        /// Returns the number of entities in this game.
        /// </summary>
        /// <returns>number of entities</returns>
        public static int Count()
        {
            return Database.Instance.EntityCount;
        }

        /// <summary>
        /// Returns the number of entities in given scene.
        /// </summary>
        /// <param name="scene">scene to inspect</param>
        /// <returns>number of entities</returns>
        public static int Count(Scene scene)
        {
            return Database.Instance.EntityCountInScene(scene);
        }

        #endregion

        #region Persistency Methods

        /// <summary>
        /// Takes a snap shot of this entity and all its content.
        /// </summary>
        /// <returns>a snap shot of this entity</returns>
        public EntityState SaveState()
        {
            return Database.Instance.EntitySaveState(Id);
        }

        /// <summary>
        /// Restores this entity from a given snap shot.
        /// </summary>
        /// <param name="state">snap shot to restore entity from</param>
        public void RestoreState(EntityState state)
        {
            Database.Instance.EntityRestoreState(Id, state);
        }

        #endregion

        #region Ownership Methods

        /// <summary>
        /// Assigns this entity to given scene.
        /// </summary>
        /// <param name="scene">scene to assign this entity to</param>
        public void AssignToScene(Scene scene)
        {
            var oldScene = GetHostingScene();
            if (oldScene == scene)
                return;

            SceneChangedEvent.Invoke(oldScene, SceneEventType.EntityRemoved, this);

            Database.Instance.EntityAssignToScene(Id, scene.Id);

            SceneChangedEvent.Invoke(scene, SceneEventType.EntityAdded, this);
        }

        /// <summary>
        /// Returns the scene this entity is currently assigned to.
        /// </summary>
        /// <returns>instance of hosting scene</returns>
        public Scene GetHostingScene()
        {
            int sceneId = Database.Instance.EntityGetIdOfHostingScene(Id);
            return (sceneId != IDPool.InvalidID) ? Scene.GetInstance(sceneId) : null;
        }

        /// <summary>
        /// Adds the given component to this entity.
        /// </summary>
        /// <param name="component">component to add</param>
        public void AddComponent(Component component)
        {
            if (Database.Instance.EntityHasComponent(Id, component.GetType()))
                throw new InvalidOperationException(
                    string.Format("Entity {0} already contains a component of type {1}",
                        Id, component.GetType().Name));

            component.AssignToEntity(this);
        }

        #endregion

        #region Data Access Methods

        /// <summary>
        /// Predicate of a component of given type is existent within this entity.
        /// </summary>
        /// <typeparam name="T">component type parameter</typeparam>
        /// <returns>true if component is existent, else false</returns>
        public bool Has<T>() where T : Component
        {
            return Database.Instance.EntityHasComponent(Id, typeof(T));
        }

        /// <summary>
        /// Returns the component of given type as T.
        /// (That way, its members can be directly accessed without casting.)
        /// </summary>
        /// <typeparam name="T">component type parameter</typeparam>
        /// <returns>component as T or null if not existent</returns>
        public T Get<T>() where T : Component
        {
            return Database.Instance.EntityGetComponent<T>(Id, typeof(T));
        }

        /// <summary>
        /// Gets all components in this scene.
        /// </summary>
        /// <returns>all components in this scene</returns>
        public IEnumerable<Component> GetAll()
        {
            return Database.Instance.EntityGetAllComponents(Id);
        }

        #endregion
    }
}
