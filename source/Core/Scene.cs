using System;
using System.Collections.Generic;

namespace Sol2E.Core
{
    /// <summary>
    /// A logical representation of a scene (or level) within the game.
    /// 
    /// This class doesn't contain any fields except for one single identifier.
    /// However it does contain methods for data acccess as well as lifecycle,
    /// persistency, ownership and data management. 
    /// The methods themself do not perform any actions, but wrap the functionality
    /// of the unterlying database to make it accessible in a way to be easily
    /// understood by clients.
    /// </summary>
    [Serializable]
    public class Scene
    {
        #region Fields

        // static reference to the scene which is currently active
        public static Scene Current { get; internal set; }
        // static reference to the global scene (is always existent
        public static Scene Global { get; internal set; }

        // identifier
        public int Id { get; private set; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Creates a new instance of Scene.
        /// </summary>
        /// <returns>new instance</returns>
        internal static Scene Create()
        {
            return Database.Instance.SceneCreate();
        }

        /// <summary>
        /// Destroys the given scene.
        /// </summary>
        /// <param name="scene">scene to destroy</param>
        internal static void Destroy(Scene scene)
        {
            Database.Instance.SceneDestroy(scene);
        }

        /// <summary>
        /// Internal constructor. Gets called by SceneCreate in Database.
        /// </summary>
        /// <param name="sceneId">id assigned to this scene</param>
        internal Scene(int sceneId)
        {
            Id = sceneId;
        }

        #endregion

        #region Instance Access Methods

        /// <summary>
        /// Returns the scene instance associated with the given id.
        /// </summary>
        /// <param name="sceneId">id of scene to get</param>
        /// <returns>scene associated with given id</returns>
        internal static Scene GetInstance(int sceneId)
        {
            return Database.Instance.SceneGetInstance(sceneId);
        }

        /// <summary>
        /// Returns a list of all scenes in this game.
        /// </summary>
        /// <returns>list of all scenes</returns>
        internal static IEnumerable<Scene> GetAllInstances()
        {
            return Database.Instance.SceneGetAllInstances();
        }

        /// <summary>
        /// Returns the number of scenes in this game.
        /// </summary>
        /// <returns>number of scenes</returns>
        public static int Count()
        {
            return Database.Instance.SceneCount;
        }

        #endregion

        #region Persistency Methods

        /// <summary>
        /// Takes a snap shot of this scene and all its content.
        /// </summary>
        /// <returns>a snap shot of this scene</returns>
        internal SceneState SaveState()
        {
            return Database.Instance.SceneSaveState(Id);
        }

        /// <summary>
        /// Restores this scene from a given snap shot.
        /// </summary>
        /// <param name="state">snap shot to restore scene from</param>
        internal void RestoreState(SceneState state)
        {
            Database.Instance.SceneRestoreState(Id, state);
        }

        #endregion

        #region Ownership Methods

        /// <summary>
        /// Adds the given entity to this scene.
        /// </summary>
        /// <param name="entity">entity to add</param>
        public void AddEntity(Entity entity)
        {
            entity.AssignToScene(this);
        }

        #endregion

        #region Data Access Methods

        /// <summary>
        /// Predicate of given entity is existent within this scene.
        /// </summary>
        /// <param name="entityId">id of entity to look for</param>
        /// <returns>true if entity is existent, else false</returns>
        public bool Has(int entityId)
        {
            return Database.Instance.SceneHasEntity(Id, entityId);
        }

        /// <summary>
        /// Gets all entities in this scene.
        /// </summary>
        /// <returns>all entities in this scene</returns>
        public IEnumerable<Entity> GetAll()
        {
            return Database.Instance.SceneGetAllEntities(Id);
        }

        /// <summary>
        /// Gets only those entities in this scene, which own a component of given type.
        /// </summary>
        /// <param name="componentType">types, which is required to exist within the entity</param>
        /// <returns>entities which own a component of given type</returns>
        public IEnumerable<Entity> GetAllWith(Type componentType)
        {
            return Database.Instance.SceneGetAllEntitiesWith(Id, componentType);
        }

        /// <summary>
        /// Gets only those entities in this scene, which own all components of given types.
        /// </summary>
        /// <param name="componentTypes">list of types, which are required to exist within the entity</param>
        /// <returns>entities which own all components of given types</returns>
        public IEnumerable<Entity> GetAllWith(IEnumerable<Type> componentTypes)
        {
            return Database.Instance.SceneGetAllEntitiesWith(Id, componentTypes);
        }

        /// <summary>
        /// Gets all components in this scene, which are of type T.
        /// </summary>
        /// <typeparam name="T">component type parameter</typeparam>
        /// <returns>all components of type T in this scene</returns>
        public IEnumerable<T> GetAllComponents<T>() where T : Component
        {
            return Database.Instance.SceneGetAllComponents<T>(Id, typeof(T));
        }

        #endregion
    }
}
